# Audio System Manual

This document describes the audio system currently used by Orbital Escape. It covers configuration, runtime ownership, music playback, modal-window processing, event-driven sound effects, and the workflow for adding audio without coupling gameplay code to Unity `AudioSource` components.

## Architecture Overview

The audio flow is intentionally split into three layers:

1. Gameplay and UI code raise domain events or request a high-level audio operation.
2. Scene audio controllers translate those events into music or `SfxType` requests.
3. The global `AudioService` resolves the configured clip and controls the runtime audio sources.

```text
Gameplay event                 Scene controller                 Global service
PlayerDiedEvent        ->      GameplayAudioController   ->     PlaySfx(Death)
PlayerJumpedEvent      ->      GameplayAudioController   ->     PlaySfx(Jump)
RestartRequestedEvent ->      GameplayAudioController   ->     PlaySfx(Restart)
UIButtonSoundRequestedEvent -> UIAudioController         ->     PlaySfx(UIFocus/UIClick)

MainMenu scene         ->      MainMenuAudioController   ->     PlayMainMenuMusic()
Gameplay scene         ->      GameplayAudioController   ->     PlayGameplayMusic(Difficulty)
```

Gameplay systems must not create or control `AudioSource` components directly. They should expose gameplay facts as events and let an audio controller decide how those facts sound.

## Main Types

| Type | Responsibility |
|---|---|
| `GameAudioConfig` | ScriptableObject containing clips, mixer references, volumes, fades, and obscured-music settings. |
| `MusicProvider` | Extensible ScriptableObject strategy that chooses a clip and playback start time. |
| `RandomTrackMusicProvider` | Chooses one non-null clip uniformly from a freely sized track list. |
| `RandomTimestampMusicProvider` | Plays one configured clip from a randomly selected millisecond timestamp. |
| `MusicPlaybackSelection` | Immutable provider result containing the selected clip and start time. |
| `DifficultyMusicProfile` | Maps one difficulty to a `MusicProvider`. |
| `IAudioService` | Public high-level API for music, SFX, music obscuring, and runtime volume changes. |
| `AudioService` | Persistent implementation that owns the runtime music and SFX sources. |
| `SfxType` | Identifies configured one-shot effects: `Jump`, `Death`, `Restart`, `UIFocus`, and `UIClick`. |
| `MainMenuAudioController` | Starts Main Menu music when the Main Menu scene is initialized. |
| `GameplayAudioController` | Starts Gameplay music and maps gameplay events to SFX. |
| `UIAudioController` | Globally subscribes to button sound requests and forwards them to the persistent SFX service. |
| `UIButtonAudioBehaviour` | Shared button component that requests focus/click sounds without owning an audio source. |
| `AutomaticUISelectionEventData` | Marks initial/restored window selection so it remains silent. |

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
| `Main Menu Music Provider` | `PlayMainMenuMusic` | Unassigned. |
| `Gameplay Music Profiles` | `PlayGameplayMusic` | Easy and Medium reference their respective random-timestamp providers; Hard, Impossible, and Nightmare have no provider assigned. |
| `JumpSfx` | `SfxType.Jump` | Unassigned. |
| `DeathSfx` | `SfxType.Death` | Assigned to `death_sound_1.wav`. |
| `RestartSfx` | `SfxType.Restart` | Unassigned. |
| `UIFocusSfx` | Mouse hover and keyboard/controller button selection | Unassigned by default; assign the desired focus clip. |
| `UIClickSfx` | Button click or navigation submit | Unassigned by default; assign the desired click clip. |

An unassigned SFX slot is valid. `PlaySfx` returns without playing anything and does not log a warning. This allows audio hooks to be implemented before final sound assets exist.

Each gameplay difficulty references one provider asset. A missing provider or a provider that cannot produce a clip calls `StopMusic`, fading out and clearing the currently playing music. Providers contain their own selection data and strategy, so difficulties are not coupled to one selection algorithm.

`RandomTrackMusicProvider` accepts any number of clips, including zero. Null entries are ignored. Every request selects one available clip uniformly at random; selecting the same clip on consecutive runs is allowed.

`RandomTimestampMusicProvider` contains one clip and any number of start timestamps in milliseconds. Every request selects one timestamp uniformly at random. An empty timestamp list starts at `0 ms`. Negative timestamps are clamped to zero, while timestamps beyond the clip duration are clamped to its final playable sample.

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

`MusicStartFadeDuration` controls the fade from silence when a track starts or restarts, including playback from a random timestamp. It defaults to `0.6 s` with gentle sine easing; set it to `0` for immediate playback. Tune it in `GameAudioConfig` independently of `MusicFadeDuration`, which controls stopping and volume transitions. Music tweens use DOTween with unscaled updates, so transitions continue while `Time.timeScale` is zero.

`PlayMainMenuMusic(restart)` behaves as follows:

- Requests a selection from the configured Main Menu provider.
- Stops music when the selected clip is unassigned.
- Keeps the current track untouched when the same clip is already playing and `restart` is `false`.
- Restarts from the provider-selected timestamp when `restart` is `true`.
- Starts a different or stopped clip from the selected timestamp and fades it to the target volume.
- Applies the current low-pass state before playback begins.

`MainMenuAudioController` and `GameplayAudioController` currently pass `restart: true`, so each scene applies a fresh provider selection instead of preserving the previous playback position.

`PlayGameplayMusic(difficulty, restart)` requests a selection from that difficulty's provider and then follows the same playback rules. The selected start time is converted from milliseconds to audio samples before playback. Provider selection has no history, so immediate repetition is possible.

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

### Shared UI Buttons

All 17 buttons in Main Menu, Difficulty Selection, Pause, Restart, and Cheat Command are nested instances of `Assets/Resources/Prefabs/UI/Controls/button.prefab`. Label text, Unitblock font/material, dimensions, background, and other appearance differences remain instance overrides. Preserve the instance names: window views use them to resolve their controls.

Assign `UIFocusSfx` and `UIClickSfx` in `GameAudioConfig` to enable sounds for every shared button. No per-button clip reference or `AudioSource` is needed. Empty slots are supported silently.

`UIButtonAudioBehaviour` automatically resolves its local `Button`:

- Mouse enter or navigation selection requests `UIFocus`. Notifications in the same frame are coalesced.
- Pointer-click selection does not request another focus sound.
- `BaseView` passes `AutomaticUISelectionEventData` when selecting or restoring a default control, including after Pause blur capture and Cheats busy operations. These selections are silent.
- The shared prefab has one persistent `Button.onClick` listener for `PlayClickSound`. It runs before runtime window actions, including actions that close the window or change scenes. Mouse click and navigation submit both use this same listener; do not add another pointer-click or submit audio handler.
- Disabled, hidden, or non-interactable controls do not request sound.

Requests travel through `UIButtonSoundRequestedEvent` to `UIAudioController`, bound globally by `LauncherInstaller`. It subscribes during initialization and unsubscribes during disposal. UI sounds use the existing persistent SFX source, continue while paused, survive window/scene changes, and are not affected by the music low-pass filter.

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

## Configuring Music Providers

### Random Track List

1. Import tracks under the appropriate `Assets/Audio/Music` folder.
2. Create `Scriptable Objects > Audio > Random Track Music Provider`.
3. Add any number of clips to its Tracks array.
4. Assign the provider to the target difficulty in `GameAudioConfig.asset`.

### Random Start Timestamp

1. Create `Scriptable Objects > Audio > Random Timestamp Music Provider`.
2. Assign its Track.
3. Add start positions to Start Timestamps Milliseconds, for example `0`, `30000`, and `62500`.
4. Assign the provider to the target difficulty or the Main Menu provider slot in `GameAudioConfig.asset`.

The provider Inspector includes an Editor-only Audio Preview section. Select one configured timestamp, optionally adjust the Preview Timestamp field, then use Play From Timestamp and Stop to audition the track without entering Play Mode. The preview position is expressed in milliseconds and does not modify the provider's configured timestamp unless the array itself is edited.

No code change is needed when switching between existing provider strategies. New selection behavior should be implemented as another `MusicProvider` ScriptableObject; `AudioService` remains responsible for playback details.

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

Confirm the destination scene has a provider assigned and that provider can produce a clip. A missing or empty provider intentionally calls `StopMusic`.

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
