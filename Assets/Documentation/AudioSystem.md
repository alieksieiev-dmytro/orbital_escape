# Audio System Manual

This document describes the audio system currently used by Orbital Escape. It covers configuration, runtime ownership, music playback, modal-window processing, event-driven sound effects, and the workflow for adding audio without coupling gameplay code to Unity `AudioSource` components.

## Architecture Overview

The audio flow is intentionally split into three layers:

1. Gameplay and UI code raise domain events or request a high-level audio operation.
2. Scene audio controllers translate those events into `MusicTrackType` or `SfxType` values.
3. The global `AudioService` resolves the configured clip and controls the runtime audio sources.

```text
Gameplay event                 Scene controller                 Global service
PlayerDiedEvent        ->      GameplayAudioController   ->     PlaySfx(Death)
PlayerJumpedEvent      ->      GameplayAudioController   ->     PlaySfx(Jump)
RestartRequestedEvent ->      GameplayAudioController   ->     PlaySfx(Restart)

MainMenu scene         ->      MainMenuAudioController   ->     PlayMusic(MainMenu)
Gameplay scene         ->      GameplayAudioController   ->     PlayMusic(Gameplay)
```

Gameplay systems must not create or control `AudioSource` components directly. They should expose gameplay facts as events and let an audio controller decide how those facts sound.

## Main Types

| Type | Responsibility |
|---|---|
| `GameAudioConfig` | ScriptableObject containing clips, mixer references, volumes, fades, and obscured-music settings. |
| `IAudioService` | Public high-level API for music, SFX, music obscuring, and runtime volume changes. |
| `AudioService` | Persistent implementation that owns the runtime music and SFX sources. |
| `MusicTrackType` | Identifies configured music tracks. Currently `MainMenu` and `Gameplay`. |
| `SfxType` | Identifies configured one-shot effects. Currently `Jump`, `Death`, and `Restart`. |
| `MainMenuAudioController` | Starts Main Menu music when the Main Menu scene is initialized. |
| `GameplayAudioController` | Starts Gameplay music and maps gameplay events to SFX. |

The runtime implementation is under `Assets/Scripts/Core/Audio`. The config asset is `Assets/Resources/Configs/GameAudioConfig.asset`.

## Lifetime And Injection

`LauncherInstaller` loads `GameAudioConfig` from `Resources/Configs/GameAudioConfig` and binds `AudioService` as a single non-lazy service.

On initialization, `AudioService` creates one `DontDestroyOnLoad` root with two child objects:

```text
AudioService
    Music
        AudioSource
        AudioLowPassFilter
    SFX
        AudioSource
```

Keeping the sources on separate objects is required because Unity audio filters process sources on the same GameObject. The low-pass filter therefore affects music only; one-shot SFX remain clear while Pause, Restart, or Cheat Command is open.

The service survives scene changes, so music transitions and one-shot effects are not destroyed during scene loading. Repeated scene loads do not create additional audio roots because the service belongs to the persistent launcher context.

The service is disposed when its owning Zenject context is destroyed. Disposal kills active audio tweens and destroys the runtime root.

## Configuration

Open `Assets/Resources/Configs/GameAudioConfig.asset` in the Inspector to configure the system.

### Clip Slots

| Property | Used by | Current state |
|---|---|---|
| `MainMenuMusic` | `MusicTrackType.MainMenu` | Unassigned. |
| `GameplayMusic` | `MusicTrackType.Gameplay` | Assigned to `gameplay_bgm_fight_looped.wav`. |
| `JumpSfx` | `SfxType.Jump` | Unassigned. |
| `DeathSfx` | `SfxType.Death` | Assigned to `death_sound_1.wav`. |
| `RestartSfx` | `SfxType.Restart` | Unassigned. |

An unassigned SFX slot is valid. `PlaySfx` returns without playing anything and does not log a warning. This allows audio hooks to be implemented before final sound assets exist.

An unassigned music slot behaves differently: requesting that track calls `StopMusic`, fading out and clearing the currently playing music.

### Mixer And Volume

| Property | Purpose |
|---|---|
| `AudioMixer` | Reserved mixer reference for project-level audio routing. |
| `MusicMixerGroup` | Optional output group assigned to the music source. |
| `SfxMixerGroup` | Optional output group assigned to the SFX source. |
| `MasterVolume` | Global multiplier applied to music and SFX. |
| `MusicVolume` | Music-only multiplier. |
| `SfxVolume` | SFX-only multiplier. |

The current config does not assign a mixer or mixer groups. Audio therefore plays directly through the generated sources, with source-level volume multipliers.

Effective volumes are calculated as:

```text
Normal music volume   = MasterVolume * MusicVolume
Obscured music volume = MasterVolume * MusicVolume * ObscuredMusicVolumeMultiplier
SFX one-shot volume   = MasterVolume * SfxVolume
```

The runtime setters on `IAudioService` change the active session only. They do not modify or save the ScriptableObject.

### Music Transitions

`MusicFadeDuration` controls fade-in and fade-out time. Music tweens use DOTween with unscaled updates, so transitions continue while `Time.timeScale` is zero.

`PlayMusic(trackType, restart)` behaves as follows:

- Resolves the clip through `GameAudioConfig.GetMusicClip`.
- Stops music when the selected clip is unassigned.
- Keeps the current track untouched when the same clip is already playing and `restart` is `false`.
- Restarts from time zero when `restart` is `true`.
- Starts a different or stopped clip from time zero and fades it to the target volume.
- Applies the current low-pass state before playback begins.

`MainMenuAudioController` and `GameplayAudioController` currently pass `restart: true`, so each scene starts its track from the beginning.

## Music Behind Modal Windows

Pause, Restart, and Cheat Command windows call `SetMusicObscured(true)` while visible. The service then:

- Fades music volume to `ObscuredMusicVolumeMultiplier`.
- Fades the music low-pass cutoff to `ObscuredMusicLowPassCutoff`.
- Applies `ObscuredMusicLowPassResonance`.
- Leaves the separate SFX source unchanged.

Closing a modal calls `SetMusicObscured(false)`, restoring normal volume and a `22000 Hz` cutoff. Both volume and filter transitions use `ObscuredMusicTransitionDuration` and unscaled DOTween updates.

Current default obscured settings are:

| Property | Value |
|---|---:|
| Volume multiplier | `0.22` |
| Low-pass cutoff | `850 Hz` |
| Low-pass resonance | `1.4` |
| Transition duration | `0.2 s` |

`SetMusicObscured` stores one Boolean state. It is not a stack or reference counter. Controllers that replace one modal with another should complete the old window and initialize the new window in the same flow, as Pause and Cheat Command currently do.

## Sound Effects

All SFX use `AudioSource.PlayOneShot`. Multiple effects may overlap on the same generated SFX source. SFX playback is not affected by `Time.timeScale` and is not filtered by the music low-pass filter.

### Current Event Mapping

| Event | Raised by | SFX |
|---|---|---|
| `PlayerJumpedEvent` | `JumpBehaviour`, after an accepted jump | `SfxType.Jump` |
| `PlayerDiedEvent` | `PlayerController`, after the life-state guard accepts a lethal collision | `SfxType.Death` |
| `RestartRequestedEvent` | `RestartCommand`, before scene reload | `SfxType.Restart` |

`GameplayAudioController` registers all three bindings during initialization and deregisters them during disposal.

### Death Sound Flow

The death SFX is configured through the existing event-driven hook:

```text
Spike or active Laser Gate collision
    -> PlayerLifeState.TryKill()
    -> PlayerDiedEvent
    -> GameplayAudioController
    -> IAudioService.PlaySfx(SfxType.Death)
    -> GameAudioConfig.DeathSfx
```

The life-state guard prevents duplicate death handling. The sound request is raised immediately when death is accepted; it does not wait for `PlayerDeathAnimation` or the Restart window.

To replace the sound later, import another clip and assign it to `DeathSfx`. No gameplay, death-animation, or controller code needs to change.

## Audio Asset Organization

Audio assets live under `Assets/Audio` and should be grouped by purpose:

```text
Assets/Audio/
    Music/
        Gameplay/
        MainMenu/
    SFX/
        Player/
        UI/
        Obstacles/
```

The project currently contains Gameplay music and Player death SFX. Add other folders when their first assets are introduced rather than committing empty directories.

The current Gameplay BGM importer uses:

| Import setting | Value |
|---|---|
| Load Type | Streaming |
| Compression Format | Vorbis |
| Quality | `0.7` |
| Preload Audio Data | Disabled |
| Load In Background | Enabled |
| Force To Mono | Disabled |
| 3D Sound | Disabled |

These settings are appropriate for long music. Short SFX should normally use Decompress On Load, preload audio data, and mono only when the source does not need stereo information. Treat those as per-asset decisions and verify memory and audible quality in the target build.

## Adding A New SFX

Use this workflow when introducing a new gameplay sound:

1. Add a new value to `SfxType`.
2. Add a serialized `AudioClip` property to `GameAudioConfig`.
3. Map the enum value in `GameAudioConfig.GetSfxClip`.
4. Reuse an existing domain event or add a new `IEvent` in its own file.
5. Raise that event only after the gameplay action succeeds.
6. Subscribe in the appropriate scene audio controller and call `PlaySfx`.
7. Import the clip under `Assets/Audio/SFX` and assign it in `GameAudioConfig.asset`.

Do not inject `AudioSource`, `AudioClip`, or `GameAudioConfig` into gameplay mechanics. The gameplay layer should report what happened, while the audio layer decides which clip represents it.

If a sound needs spatial positioning, looping ownership, random variants, pitch variation, or concurrency limits, extend `IAudioService` with a high-level operation rather than bypassing it from gameplay code.

## Adding A New Music Track

1. Add a value to `MusicTrackType`.
2. Add the clip property and `GetMusicClip` mapping to `GameAudioConfig`.
3. Import the track under the appropriate `Assets/Audio/Music` folder.
4. Assign the clip in `GameAudioConfig.asset`.
5. Request the track from a scene-level controller with the desired restart behavior.

Music should be selected at scene or game-flow boundaries. Avoid requesting tracks from individual views or gameplay obstacles.

## Troubleshooting

### An expected SFX is silent

1. Confirm the matching clip slot in `GameAudioConfig.asset` is assigned.
2. Confirm the gameplay action raises its event only after succeeding.
3. Confirm the active scene binds its audio controller.
4. Confirm `MasterVolume` and `SfxVolume` are above zero.
5. If a mixer group is assigned, confirm that group is not muted.

An empty clip does not produce a warning by design.

### Music stops when changing scenes

Confirm the destination scene's track slot is assigned. Requesting an unassigned music type intentionally calls `StopMusic`.

### Music stays muffled

Check that every controller calling `SetMusicObscured(true)` also calls `SetMusicObscured(false)` in `Complete`. Because the state is Boolean, an interrupted modal lifecycle can leave music obscured.

### Duplicate sounds play

Confirm the source gameplay event is guarded against duplicate actions and that the audio controller deregisters its binding during disposal. Do not add a second subscriber for the same sound unless layered playback is intentional.

### Audio disappears during scene reload

Use `IAudioService.PlaySfx` instead of a scene-owned `AudioSource`. The persistent global SFX source allows Restart SFX and similar effects to survive scene loading.

## Validation Checklist

After changing the audio system:

1. Build `Assembly-CSharp-Editor.csproj` with zero errors.
2. Validate Launcher, Main Menu, and Gameplay Zenject bindings.
3. Test with assigned and empty clip slots.
4. Verify repeated scene loads do not create duplicate `AudioService` roots.
5. Verify Pause, Restart, and Cheat Command obscure and restore music.
6. Verify one-shot SFX survive pause and scene reload where expected.
7. Confirm no gameplay class directly owns an `AudioSource`.
