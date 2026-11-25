# Phase 2 — Systems Vertical Slice

## Advanced Mechanics Checkpoint

**Course:** SWE 402 – Game Development  
**Game Title:** Buried Outside  
**Genre:** Psychological Horror / Escape Room  
**Team:** Forkan, Zahra, and Kawthar  
**Unity Version:** Unity 6.5 (6000.5.9f1), Universal Render Pipeline, Windows  
**Playable Build:** [Unity project repository](https://github.com/kawthar1235/project-swe402). Testing was conducted in the Unity Editor; no standalone build is included in this checkpoint.  
**Checkpoint Date:** 25 November 2026

---

## 1. Playable Slice Summary

### 1.1 Game Overview

Buried Outside is a horror escape game in which the player searches an interior environment, follows clues, solves connected puzzles, and faces a monster. Our Phase 2 work focused on making the room mechanics and encounter function together. Development and internal testing took approximately one week.

### 1.2 Current Playable Scope

The prototype includes a start menu, two connected puzzle/challenge rooms, object handling, doors, a timed encounter, and flashlight-based monster defeat. The current end condition displays a victory message and removes the monster.

The intended ending goes further: a door with light appears after the encounter, and the player enters it to finish Level 1. That exit sequence is not implemented. The latest test also found that winning returns the player to the checkpoint, although this should happen only after losing. The current slice therefore has working progression and outcome triggers, but its final state handling is incomplete.

### 1.3 Core Gameplay Loop

The player explores, interprets a clue, uses the relevant object, and applies the result to progress. The final encounter asks the player to use the flashlight clue shown on the television.

**Gameplay Flow:**

Start menu → straighten painting → collect record → insert record into gramophone → reverse playback to hear **7392** → submit code on chest → collect key → unlock first door → enter second room → view TV hint → complete timed door sequence → face monster → use flashlight → current victory feedback.

The timed door closes on entry, remains locked for 30 seconds, and then reopens. The intended lit exit after victory remains planned.

### 1.4 Win and Fail Conditions

| Condition | Description | Implementation Status |
|---|---|---|
| Win Condition | Direct the flashlight at the monster; it disappears and victory feedback appears. | Implemented, but incorrectly followed by checkpoint return. |
| Fail Condition | The monster catches the player. | Loss trigger implemented; final failure UI unfinished. |
| Restart | Return the player to a checkpoint in front of the first puzzle-room door. | Works after loss; must be separated from victory. Full repeated-run reset coverage is not documented. |

### 1.5 Known Limitations

- The illuminated exit and final Level 1 completion sequence are missing.
- Victory currently returns the player to the checkpoint instead of keeping a completed state.
- Monster pursuit is not reliable enough, and character movement feels difficult.
- Text prompts reveal interactions too directly, making the puzzles easier than intended.
- Settings, hint presentation, and the remaining UI are incomplete.
- TV audio is still too loud; background ambience and remaining object sounds need work.
- Monster disappearance lacks the planned evaporation effect.
- Standalone build testing, performance measurements, and recorded test evidence are not included.

---

## 2. Systems Implementation Status

### 2.1 Player Controls

The player uses Rigidbody-based movement. Movement and turning are applied in physics updates, while the camera provides the view and flashlight aiming direction. The latest internal test identified control comfort as an issue, so responsiveness and difficulty values need another tuning pass.

| Input | Action |
|---|---|
| W / S | Move forward / backward. |
| A / D | Turn the player. |
| Mouse | Look and aim while camera-look mode is active. |
| Left mouse button | Interact with objects, doors, and puzzle controls. |
| Space | Drop a held object. |
| E | Turn the held flashlight on or off. |
| Gramophone controls | Play normally, reverse playback, or stop. |
| Keypad buttons and Enter | Enter and submit a four-digit code. |

### 2.2 Interaction System

Interactive objects use `IInteractable`, with raycasts identifying the targeted object. A camera-attached hold point positions the record, key, and flashlight. Colliders are disabled while an item is held and restored when it is dropped. The key is consumed on the first door unlock; the unlocked door can then be opened and closed without another key.

The TV has a separate screen surface for its picture and effects. Its interaction has needed revisions after integration. Click detection, screen activation, and hover feedback must be checked together, since a correct script alone does not confirm correct scene references.

### 2.3 Physics and Collision

The player uses a Rigidbody and body collider. Walls and door panels use solid colliders, while the room-entry area uses a trigger. Raycasts should stop at solid surfaces rather than allowing interaction through walls or furniture.

Development fixes addressed missing colliders, trigger settings, and layer collision settings. Doors rotate around hinge markers, with the double door using opposite rotations for its panels. The collider moves with each door panel. A complete wall-by-wall regression test is still required after scene merges.

### 2.4 Puzzle and Challenge Logic

| Puzzle | Description | Success Condition | Status |
|---|---|---|---|
| Painting and record | Adjust the painting to access the record. | The record becomes available for collection. | Implemented; refined by Zahra and Forkan. |
| Gramophone | Insert the carried record and reverse its playback. | The player understands the code 7392. | Implemented; clue audio prepared by Kawthar. |
| Chest keypad | Use the physical number buttons and submit the code. | Enter 7392 and press Enter to reveal the key. | Implemented; invalid entries clear for another attempt. |
| Key and first door | Carry the key to the locked door. | The key is consumed and the door unlocks. | Implemented. |
| TV and timed encounter | View the flashlight clue before facing the monster. | Apply the clue by directing light at the monster. | Core behavior implemented; pursuit, feedback, and audio need refinement. |

### 2.5 AI and Threat Behavior

Zahra integrated the monster scripts, animation, and win/loss interactions. The behavior can be described using the states below. These describe gameplay behavior rather than a claim that a particular FSM class or behavior-tree framework is used.

| State | Behavior | Transition Condition |
|---|---|---|
| Waiting | The monster waits for the encounter. | The room sequence allows the encounter to begin. |
| Active encounter | The monster enters and attempts to pursue the player. | It catches the player or is defeated by flashlight exposure. |
| Player caught | Trigger the loss response and checkpoint return. | The player is placed back at the checkpoint. |
| Defeated | Remove the monster and show victory feedback. | Flashlight defeat condition is met. |

The latest test found that pursuit does not consistently follow the player as intended. Improving chase behavior and separating victory from failure recovery are high-priority fixes.

### 2.6 Game-State Management

The current flow connects menu entry, exploration, puzzle progression, the timed encounter, loss, retry, and victory feedback. Checkpoint return is implemented, but victory currently uses a recovery path that also returns the player. The intended flow is to respawn only after loss and keep victory active until the player enters the future exit door.

### 2.7 Systems Integration

The painting enables the record, the record enables the audio clue, the clue solves the chest, and the key unlocks progression. The second room connects the TV hint, door timer, monster, flashlight, and outcome logic. Integration work also involved camera references, hold points, colliders, lighting, animation, and scene settings.

### 2.8 Implementation Summary

The evidence column identifies source locations. It does not replace runtime proof or a recorded playtest.

| System | Status | Responsible Member | Evidence |
|---|---|---|---|
| Player Controls | Implemented; tuning needed. | Forkan and Zahra | `Assets/Script/PlayerMovement.cs`, `CameraFollow.cs` |
| Interaction System | Implemented; consistency and regression checks needed. | Forkan and Zahra | `PlayerInteraction.cs`, `ChestPuzzleTools.cs`, object interaction scripts |
| Physics and Collision | Collider fixes applied; full regression check pending. | Forkan and Zahra | Scene colliders and `ProjectSettings/DynamicsManager.asset` |
| Puzzle Logic | Connected room puzzles implemented. | Forkan and Zahra | Painting, record, gramophone, chest, and door scripts |
| AI / Threat Logic | Integrated; pursuit needs improvement. | Zahra | Monster scripts and scene animation setup |
| Game-State Management | Win/loss triggers implemented; victory return is incorrect. | Zahra | `GameManager.cs` and monster outcome logic |
| UI System | Start menu implemented; remaining interface incomplete. | Kawthar | Menu/interface assets in the repository |
| Audio System | Puzzle audio and first-pass playback available; mix incomplete. | Kawthar: clue audio; Forkan: other audio | Gramophone and TV playback scripts and audio assets |

---

## 3. UI and Audio — First Pass

### 3.1 User Interface and Feedback

Kawthar implemented the start menu. Puzzle feedback includes keypad digits, playback controls, and first-pass interaction prompts. The timed sequence has a final five-second countdown, red pulse, and subtle noise. The latest feedback suggests that some prompts explain too much and should be replaced with clearer but less revealing interaction cues.

| UI Element | Purpose | Status |
|---|---|---|
| Start menu | Begin the experience. | Implemented by Kawthar. |
| Interaction Prompt | Identify usable objects. | First pass; some wording reveals too much. |
| Puzzle Feedback | Show keypad input, submission feedback, and playback controls. | Implemented first pass. |
| HUD | Present consistent gameplay instructions and state feedback. | Incomplete. |
| Victory Screen | Confirm success and support level completion. | Basic message appears; final UI and exit flow unfinished. |
| Failure Screen | Explain failure and support retry. | Checkpoint behavior exists; final UI unfinished. |
| Settings and hints | Support player preferences and guidance. | Planned. |

### 3.2 Audio Implementation

| Audio Element | Purpose | Trigger | Status |
|---|---|---|---|
| Ambient Audio | Establish the room's atmosphere. | Gameplay in the environment. | Remaining audio pass. |
| Sound Effects | Confirm object actions and puzzle responses. | Object use, keypad input, and door events. | First-pass support; remaining clips and balancing unfinished. |
| Reversed Audio | Reveal the understandable code clue. | Reverse playback control on the gramophone. | Implemented. |
| Normal Audio | Present the initially unclear recording. | Normal playback control. | Implemented. |
| Television static | Support the old-TV appearance. | TV activation. | Implemented first pass; volume still too high. |
| Background Music | Support atmosphere and tension. | Planned gameplay cues. | Not yet integrated. |

### 3.3 Audio Technical Design

AudioSource and AudioClip components handle playback. The gramophone switches between playback modes, while the TV combines its image effect with sound and a local light. The TV uses spatial audio to associate sound with the object. Kawthar prepared the puzzle recording; Forkan worked on other sound integration and will continue the audio pass. Volume and spatial range need further listening tests.

### 3.4 Planned UI and Audio Improvements

Complete settings and hint presentation, make prompts consistent without revealing puzzle solutions, and finish outcome screens. Reduce TV volume in the actual scene Inspector, since a serialized value can remain unchanged after a script default is edited. Add ambience and object sounds, then balance them against the spoken code and monster encounter.

---

## 4. Graphics and Technical Art

### 4.1 Environment and Materials

Kawthar selected and prepared the map. Forkan placed characters and worked on the interactive room setup. The environment uses imported interior assets, furniture, and horror props. Although it is more detailed than a primitive graybox, this checkpoint evaluates the connected mechanics rather than final visual polish.

### 4.2 Lighting and Rendering

The project uses Universal Render Pipeline materials and lighting. Forkan and Zahra worked on lighting adjustments. Flashlight illumination supports the monster mechanic, while a local TV light supports the screen effect. Unity reported shadow-atlas warnings during development; these need investigation through profiling rather than being treated as a measured bottleneck.

### 4.3 Animation, VFX, and Camera

Zahra linked animation and monster behavior to the gameplay scripts. Camera-relative holding keeps carried objects in view, and a close camera view supports keypad use. The TV uses startup static, scanlines, and picture noise. Remaining work includes monster evaporation and the illuminated exit. The latest control feedback also indicates that camera and movement comfort need adjustment. No completed post-processing pass is claimed.

---

## 5. Playtest Report

### 5.1 Test Objectives

Check whether players can progress through the connected puzzles, use the flashlight, trigger win/loss behavior, and return to the checkpoint. We also considered movement comfort, clue difficulty, enemy pursuit, and audio balance.

### 5.2 Test Setup

| Item | Details |
|---|---|
| Test Date | Latest internal test: 25 November 2026. |
| Number of Testers | Three internal team members. |
| Tester Names / IDs | Forkan, Zahra, and Kawthar. |
| Build Version / Commit | Exact tested commit is not provided in this report. |
| Testing Platform | Windows PC; hardware specifications are not included. |
| Testing Environment | Unity Editor. |
| Test Duration | Development and testing covered approximately one week; individual session durations are not included. |

These were informal team tests, not an independent usability study. Results below reflect team observations, without invented measurements, attempt counts, or individual quotations.

### 5.3 Functional Test Cases

| ID | Test Scenario | Expected Result | Actual Result | Status |
|---|---|---|---|---|
| T01 | Player Movement | Move and aim comfortably through the rooms. | Movement works but feels difficult to control. | Partial |
| T02 | Collision Detection | Walls and closed doors block the player. | Collision issues were addressed during development; a full post-merge wall sweep was not documented. | Not fully verified |
| T03 | Object Interaction | Collect and use items; operate doors and the TV. | Core interactions are implemented. TV click handling has required fixes; its latest isolated retest is not documented. | Partial |
| T04 | Puzzle Completion | Complete the connected painting, record, gramophone, chest, and key route. | The team completed this route in Editor testing. | Pass — internal test |
| T05 | Incorrect Puzzle Solution | Clear an invalid keypad submission and allow another attempt. | Implemented; a separate negative-test result was not recorded. | Not separately verified |
| T06 | Audio Playback | Hear the clue and balanced object feedback. | Puzzle playback is available; TV is too loud and other audio is incomplete. | Partial |
| T07 | AI / Threat Behavior | Pursue and threaten the player reliably. | Monster pursuit is inconsistent. | Needs fixing |
| T08 | Win Condition | Defeat the monster and remain in a completed state. | Disappearance and victory message occur, but the player also returns to the checkpoint. | Partial |
| T09 | Fail Condition | Being caught triggers loss and checkpoint return. | The player returns in front of the first puzzle-room door. | Pass — core behavior |
| T10 | Restart System | Recover after loss with valid gameplay state. | Checkpoint return works; it incorrectly also follows victory. Full reset coverage was not recorded. | Partial |
| T11 | End-to-End Gameplay | Progress from start to a clear, stable Level 1 ending. | The core route reaches victory feedback. The exit and final outcome flow are unfinished. | Partial |

### 5.4 Player Feedback

Feedback was discussed collectively. The table does not assign comments to a particular member when that attribution was not recorded.

| Tester | Positive Feedback | Problems Encountered | Suggestions |
|---|---|---|---|
| Internal team — Forkan, Zahra, Kawthar | The connected puzzle route can be completed. | Character movement feels difficult. | Tune movement, turning, and camera response. |
| Internal team | Flashlight use triggers monster defeat. | The monster does not pursue the player reliably. | Review chase behavior and tune the encounter. |
| Internal team | Interactive objects provide guidance. | Explicit text prompts make the puzzles too easy. | Use less revealing prompts and consistent visual cues. |
| Internal team | TV sound supports the effect. | Its volume is too high. | Reduce the scene value and recheck the full mix. |

### 5.5 Identified Issues and Priorities

P1 items affect progression, outcomes, or the main challenge. P2 items affect usability, balance, or presentation. These priorities are a proposed work order, not GitHub board statistics.

| Issue ID | Description | Severity | Priority | Planned Fix | Owner | Status |
|---|---|---|---|---|---|---|
| BUG-01 | Winning also returns the player to the checkpoint. | High | P1 | Separate victory completion from failure recovery. | Zahra, with team retest | Open |
| BUG-02 | Monster pursuit is inconsistent. | High | P1 | Review chase transitions, movement, and encounter values. | Zahra, with Forkan for tuning | Open |
| BUG-03 | Movement is uncomfortable. | Medium | P2 | Adjust movement, turn, and camera values. | Forkan and Zahra | Open |
| BUG-04 | TV audio remains too loud. | Medium | P2 | Lower the serialized volume and retest with other sounds. | Forkan | Open |
| BUG-05 | Prompts reveal too much of the puzzle solution. | Medium | P2 | Revise wording and visual feedback with the UI pass. | Kawthar and Forkan | Planned |
| BUG-06 | Interaction/collision changes can regress after merges. | High | P1 | Verify references and repeat the complete route on the submission commit. | Team | Retest required |

### 5.6 Applied Fixes and Retesting

| Issue | Applied Fix | Retest Result | Evidence |
|---|---|---|---|
| Door animation conflicts | Replaced relevant door movement with hinge-based rotation and moving-panel colliders. | Door operation was checked during development; no capture is attached. | Door scripts and scene setup. |
| Missing or unsuitable colliders | Added or corrected colliders and checked trigger/layer settings. | Full final-scene coverage has not been documented. | Scene and physics settings. |
| TV screen and click handling | Revised screen state, click handling, image effects, and hover feedback. | Latest isolated retest outcome is not recorded. | `TVInteraction.cs`; no runtime capture. |
| Merge and integration errors | Zahra corrected integration problems and script/animation links. | Integration remains a regression risk; no formal retest log is attached. | Repository history and project files. |

### 5.7 Playtest Conclusion

The prototype demonstrates the connected puzzle route, flashlight defeat, and checkpoint return. It is not yet stable enough to describe every system as complete. The next fixes should separate win and loss recovery, improve pursuit, and tune controls. Presentation work should then address prompts, audio, defeat VFX, and the intended exit. A recorded end-to-end retest is still needed.

---

## 6. Performance and Debugging

### 6.1 Performance Profiling

Testing focused on functionality. No Unity Profiler session, performance capture, or hardware record is included. We therefore cannot claim a target frame rate or quantify a bottleneck.

| Metric | Observed Value | Notes |
|---|---|---|
| Average FPS | Not measured. | No session average was recorded. |
| CPU Frame Time | Not measured. | Requires a Profiler capture. |
| GPU Frame Time | Not measured. | Requires supported GPU profiling data. |
| Memory Usage | Not measured. | Requires a memory capture with test context. |

### 6.2 Bottlenecks and Stability Issues

Development exposed missing script references, a missing `FlashlightBeam` tag, package-resolution errors, and shadow-atlas warnings. This report does not claim that every historical warning is resolved in the latest version. The confirmed gameplay issues are listed in Section 5.5. TV texture updates and real-time lighting are candidates for future measurement, not proven bottlenecks.

### 6.3 Optimization Actions

The next profiling pass will compare exploration, TV playback, and the monster encounter under the same settings. Record the hardware, resolution, CPU/GPU frame times, and memory, then investigate costly samples. Retain a capture before and after any optimization. Repeat the functional route in a standalone build, since Editor observations do not establish build performance.

---

## 7. Production Update

### 7.1 Team Members and Contributions

| Member | Role | Completed Contributions |
|---|---|---|
| Forkan | Gameplay mechanics, characters, interaction, and technical-art integration | Placed characters; developed room interactions and mechanisms, object carrying, puzzle controls, doors, TV and flashlight behavior; worked on lighting and first-pass sounds. Continuing audio, effects, play-value, and difficulty adjustments. |
| Zahra | Gameplay programming, AI, animation, and integration | Made substantial script changes; linked scripts and animation; integrated the monster and win/loss interactions; refined the painting and record puzzles; worked on lighting with Forkan; resolved GitHub merge and integration errors. |
| Kawthar | Map preparation, menu/UI, and clue audio | Selected and prepared the map, implemented the start menu, and created the audio clue. Remaining settings and hint UI are planned. |

### 7.2 GitHub Project Board

**Project Board Link:** Not available for this checkpoint. The team has a [GitHub repository](https://github.com/kawthar1235/project-swe402), but a separate Project board has not been provided.

Formal board counts are unavailable. The completed and carry-over work below serves as the current documented task list; it should be moved to an owned, tracked board during the next iteration.

| Status | Number of Tasks |
|---|---|
| Done | Not formally counted; completed work is listed in Section 7.3. |
| In Progress | Not formally counted; ongoing work is listed in Section 7.4. |
| To Do | Not formally counted. |
| Blocked | Not formally counted; risks are listed in Section 7.6. |

### 7.3 Completed Tasks

Completed here means implemented in the prototype; related limitations remain visible in the test and issue tables.

| Task | Owner | GitHub Issue / PR |
|---|---|---|
| Map selection and preparation | Kawthar | Repository history; no issue/PR link recorded. |
| Start menu and puzzle recording | Kawthar | Repository history; no issue/PR link recorded. |
| Character placement and room mechanisms | Forkan | Repository source and scene changes; no issue/PR link recorded. |
| Script integration, animation, and monster outcomes | Zahra | Repository source and integration history; no issue/PR link recorded. |
| Painting/record refinements and lighting adjustments | Zahra and Forkan | Source and scene changes; no issue/PR link recorded. |
| Internal functional testing | Forkan, Zahra, Kawthar | Observations in Section 5; no recording attached. |

### 7.4 Carry-Over Tasks

| Task | Reason | Owner | Priority |
|---|---|---|---|
| Correct victory recovery and improve pursuit | Current behavior affects the encounter and ending. | Zahra, with team testing | High |
| Implement illuminated exit | Intended Level 1 ending is missing. | Team; specific owner not yet assigned | High |
| Tune movement and difficulty | Latest playtest identified control and challenge issues. | Forkan and Zahra | High |
| Complete settings, hints, and consistent prompts | Current interface is a first pass. | Kawthar, with Forkan on interaction cues | Medium |
| Add evaporation and remaining effects | Defeat works without its intended presentation. | Forkan | Medium |
| Finish ambience, object sounds, and mixing | Audio coverage and balance are incomplete. | Forkan | Medium |
| Profile and verify a standalone build | No measured performance or build test is available. | Team | High before the next review |

### 7.5 Repository and Collaboration Workflow

The team used commits, branches, pulls, pushes, and merges to integrate work. Integration showed that scripts, scene references, `.meta` files, and project settings must be reviewed together. Zahra handled significant merge and linking fixes. A structured issue/PR review record is not included, so this report does not claim formal approvals or review coverage.

For the next iteration, use feature branches, small commits, named task owners, and an integrated test before merging. Record the exact tested commit so results can be reproduced.

### 7.6 Blockers and Risks

The main risks are incorrect outcome transitions, unreliable pursuit, scene-reference regressions after merging, and differences in Editor/package settings. Missing performance and build evidence also limits confidence in stability outside the Editor. The team should freeze a test version, resolve P1 issues, check the Console, and then repeat the complete route before adding more features.

---

## 8. Phase 3 Readiness

### 8.1 Systems Ready for Phase 3

- The map, characters, connected puzzle route, and item-handling systems provide a base for further iteration.
- Flashlight-based monster defeat and loss/checkpoint triggers are implemented, with outcome handling still needing correction.
- The start menu, audio clue, TV effects, and first-pass feedback are available for refinement.

### 8.2 Planned Improvements

- **Gameplay:** Correct victory recovery, improve pursuit, tune controls, and add the final exit.
- **Graphics and Lighting:** Improve object readability and review real-time lights/shadows after measurement.
- **Animation and VFX:** Add evaporation and final exit effects; verify animation links.
- **UI/UX:** Complete settings, hints, and outcome screens; reduce solution-revealing prompts.
- **Audio:** Reduce TV volume and complete ambience, SFX, and mixing.
- **Performance:** Collect Profiler evidence and optimize confirmed costs.
- **Testing:** Record successful, failed, and repeated runs in the Editor and a standalone build.

### 8.3 Phase 3 Priorities

The sequence below is proposed; calendar completion dates have not been agreed.

| Priority | Task | Owner | Expected Completion |
|---|---|---|---|
| High | Correct win/loss separation and pursuit. | Zahra, with team retest | First Phase 3 integration pass. |
| High | Tune controls and implement the exit. | Forkan/Zahra for tuning; exit owner to be assigned | Before the next full-level test. |
| Medium | Complete UI, hints, audio, and effects. | Kawthar: UI; Forkan: audio/VFX | After core outcome fixes and before the next review. |
| High | Profile, test a build, and record evidence. | Team | Before declaring the next version stable. |

---

## 9. Competency Coverage

| Competency | Current Application | Evidence | Next Action |
|---|---|---|---|
| Core Gameplay Systems Engineering | Connected control, puzzle, item, encounter, and outcome logic. | Source references in Section 2.8; functional observations in Section 5.3. | Separate win/loss recovery and complete the exit. |
| Physics and Collision Systems | Rigidbody movement, raycasts, solid colliders, triggers, and hinged doors. | Scene setup and physics/interaction scripts. | Verify all wall and doorway responses after integration. |
| AI Behavior Design | Monster waiting, encounter, defeat, and player-caught behavior. | Monster scripts and reported encounter results. | Improve chase transitions and prevent repeated outcome triggers. |
| Real-Time Graphics Pipeline | URP materials, interior lighting, TV texture updates, and flashlight illumination. | Scene/material setup and TV/flashlight scripts. | Measure rendering cost and improve readability. |
| Technical Art and Polish | Camera-relative carrying, keypad close view, TV noise, and countdown effects. | Interaction and effect scripts; no runtime captures attached. | Add evaporation and the lit exit. |
| Audio Systems Design | Audio clue, playback controls, and initial object sounds. | Gramophone/TV scripts and audio assets. | Complete sounds and test volume/spatial balance. |
| UI/UX Feedback | Start menu, keypad feedback, countdown, and basic victory message. | UI assets and internal feedback in Section 5.4. | Finish UI and use less revealing guidance. |
| Performance Optimization and Debugging | Investigated interaction, collision, linking, and merge problems. | Issues and fixes in Sections 5–6. No profiling evidence. | Capture measurements before choosing optimizations. |
| Production and Collaboration Practices | Shared GitHub development and team testing. | Repository history and contributions in Section 7. | Establish board/issue ownership, review records, and a fixed test version. |

---

## 10. Technical Evidence

This checkpoint contains written team observations and repository source references. **No screenshots, gameplay recordings, Profiler captures, or separate playtest records are attached.** Source code can support implementation discussion but cannot demonstrate runtime reliability on its own.

| Evidence ID | Description | Screenshot / Link |
|---|---|---|
| E01 | Playable Environment | [Repository assets and scenes](https://github.com/kawthar1235/project-swe402/tree/main/Assets). No runtime capture attached. |
| E02 | Player Interaction | `Assets/Script/` and scene references; no interaction recording. |
| E03 | Puzzle Implementation | Painting, record, gramophone, chest, and door scripts; no full-route recording. |
| E04 | Audio System | Repository audio assets and playback scripts; no audio test recording. |
| E05 | Win Condition | Internal observation: monster disappears and victory feedback appears; incorrect checkpoint return remains. No capture attached. |
| E06 | Fail Condition | Internal observation: caught player returns to the checkpoint. No capture attached. |
| E07 | Performance Profiler | Not measured; no capture available. |
| E08 | GitHub Project Board | Not provided. |
| E09 | Playtest Evidence | Written internal observations in Section 5; no separate playtest recording or test log. |
| E10 | Pull Requests and Issues | [Repository](https://github.com/kawthar1235/project-swe402); specific review/issue links are not documented. |

Repository links refer to the branch and are not pinned to a tested commit. Future captures should identify the commit and be stored under `docs/phase2/evidence/`, with relative links from this file.

---

## 11. Conclusion

Phase 2 established the connected room mechanics, audio clue, key progression, timed encounter, and flashlight-based monster defeat. The team also integrated characters, animation, lighting, and a start menu. Internal testing identified specific problems with control comfort, pursuit, victory recovery, overly explicit prompts, and TV volume.

The prototype is a working foundation, but the intended Level 1 ending and several presentation systems remain incomplete. Our next work should prioritize correct outcome handling and pursuit, followed by control tuning, the illuminated exit, UI, audio, and VFX. Performance profiling, standalone testing, and recorded evidence are still required to support stronger stability claims.
