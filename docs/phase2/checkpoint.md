# Phase 2 — Systems Vertical Slice
## Advanced Mechanics Checkpoint

**Course:** SWE 402 – Game Development  
**Game Title:** [Game Name]  
**Genre:** Psychological Horror / Escape Room  
**Team:** [Team Name]  
**Unity Version:** [Version]  
**Playable Build:** [Link]  

---

## 1. Playable Slice Summary

### 1.1 Game Overview
[Briefly describe the game concept, environment, player objective, and intended experience.]

### 1.2 Current Playable Scope
[Describe what is currently playable, where the player starts, and where the playable slice ends.]

### 1.3 Core Gameplay Loop
[Explain the sequence of actions the player performs throughout the game.]

**Gameplay Flow:**

Start → [Action] → [Challenge] → [Puzzle] → [End Condition]

### 1.4 Win and Fail Conditions

| Condition | Description | Implementation Status |
|---|---|---|
| Win Condition | [How the player wins] | [Status] |
| Fail Condition | [How the player loses] | [Status] |
| Restart | [How the player retries] | [Status] |

### 1.5 Known Limitations
[Describe incomplete features, current restrictions, temporary solutions, and known gameplay limitations.]

---

## 2. Systems Implementation Status

### 2.1 Player Controls

[Describe the implemented movement, camera controls, and player input system.]

| Input | Action |
|---|---|
| [Key] | [Action] |
| [Key] | [Action] |
| [Key] | [Action] |

### 2.2 Interaction System

[Explain how the player interacts with objects, doors, keys, puzzles, and other interactive elements. Describe the interaction method and its implementation.]

### 2.3 Physics and Collision

[Explain how colliders, rigidbodies, triggers, raycasting, and collision detection are used where applicable. Describe how reliable interactions are ensured.]

### 2.4 Puzzle and Challenge Logic

[Describe each implemented puzzle, its objective, solution, and connection to player progression.]

| Puzzle | Description | Success Condition | Status |
|---|---|---|---|
| Puzzle 1 | [Description] | [Condition] | [Status] |
| Puzzle 2 | [Description] | [Condition] | [Status] |
| Puzzle 3 | [Description] | [Condition] | [Status] |

### 2.5 AI and Threat Behavior

[Describe the implemented enemy, threat, or state-driven behavior. Explain its states, triggers, and transitions. If not implemented, explain the current limitation and planned approach.]

| State | Behavior | Transition Condition |
|---|---|---|
| [State] | [Behavior] | [Condition] |
| [State] | [Behavior] | [Condition] |

### 2.6 Game-State Management

[Explain how the game manages transitions between gameplay, victory, failure, and restarting.]

### 2.7 Systems Integration

[Explain how the implemented systems communicate and work together to create a complete playable experience.]

### 2.8 Implementation Summary

| System | Status | Responsible Member | Evidence |
|---|---|---|---|
| Player Controls | [Status] | [Name] | [Link] |
| Interaction System | [Status] | [Name] | [Link] |
| Physics and Collision | [Status] | [Name] | [Link] |
| Puzzle Logic | [Status] | [Name] | [Link] |
| AI / Threat Logic | [Status] | [Name] | [Link] |
| Game-State Management | [Status] | [Name] | [Link] |
| UI System | [Status] | [Name] | [Link] |
| Audio System | [Status] | [Name] | [Link] |

---

## 3. UI and Audio — First Pass

### 3.1 User Interface and Feedback

[Describe the current HUD, interaction prompts, instructions, puzzle feedback, and win/fail screens.]

| UI Element | Purpose | Status |
|---|---|---|
| Interaction Prompt | [Purpose] | [Status] |
| Puzzle Feedback | [Purpose] | [Status] |
| HUD | [Purpose] | [Status] |
| Victory Screen | [Purpose] | [Status] |
| Failure Screen | [Purpose] | [Status] |

### 3.2 Audio Implementation

[Describe the implemented sound effects, background music, ambient sounds, and audio-based mechanics.]

| Audio Element | Purpose | Trigger | Status |
|---|---|---|---|
| Ambient Audio | [Purpose] | [Trigger] | [Status] |
| Sound Effects | [Purpose] | [Trigger] | [Status] |
| Reversed Audio | [Purpose] | [Trigger] | [Status] |
| Normal Audio | [Purpose] | [Trigger] | [Status] |
| Background Music | [Purpose] | [Trigger] | [Status] |

### 3.3 Audio Technical Design

[Explain the use of Audio Sources, Audio Clips, audio scripting, mixing, and spatial audio where applicable.]

### 3.4 Planned UI and Audio Improvements

[Describe the improvements planned for Phase 3 based on current limitations and player feedback.]

---

## 4. Graphics and Technical Art

### 4.1 Environment and Materials

[Describe the current environment, assets, materials, and graybox implementation.]

### 4.2 Lighting and Rendering

[Explain the lighting setup, rendering pipeline, and how visual readability is maintained.]

### 4.3 Animation, VFX, and Camera

[Describe the currently implemented animations, visual effects, camera design, and post-processing. Identify any features planned for Phase 3.]

---

## 5. Playtest Report

### 5.1 Test Objectives

[Explain what the playtest aims to evaluate, including gameplay functionality, usability, stability, and system integration.]

### 5.2 Test Setup

| Item | Details |
|---|---|
| Test Date | [Date] |
| Number of Testers | [Number] |
| Tester Names / IDs | [Names or IDs] |
| Build Version / Commit | [Version] |
| Testing Platform | [Platform] |
| Testing Environment | [Unity Editor / Build] |
| Test Duration | [Duration] |

### 5.3 Functional Test Cases

| ID | Test Scenario | Expected Result | Actual Result | Status |
|---|---|---|---|---|
| T01 | Player Movement | [Expected] | [Actual] | [Pass/Fail] |
| T02 | Collision Detection | [Expected] | [Actual] | [Pass/Fail] |
| T03 | Object Interaction | [Expected] | [Actual] | [Pass/Fail] |
| T04 | Puzzle Completion | [Expected] | [Actual] | [Pass/Fail] |
| T05 | Incorrect Puzzle Solution | [Expected] | [Actual] | [Pass/Fail] |
| T06 | Audio Playback | [Expected] | [Actual] | [Pass/Fail] |
| T07 | AI / Threat Behavior | [Expected] | [Actual] | [Pass/Fail] |
| T08 | Win Condition | [Expected] | [Actual] | [Pass/Fail] |
| T09 | Fail Condition | [Expected] | [Actual] | [Pass/Fail] |
| T10 | Restart System | [Expected] | [Actual] | [Pass/Fail] |
| T11 | End-to-End Gameplay | [Expected] | [Actual] | [Pass/Fail] |

### 5.4 Player Feedback

[Summarize the feedback collected from playtest participants.]

| Tester | Positive Feedback | Problems Encountered | Suggestions |
|---|---|---|---|
| Tester 1 | [Feedback] | [Problems] | [Suggestions] |
| Tester 2 | [Feedback] | [Problems] | [Suggestions] |
| Tester 3 | [Feedback] | [Problems] | [Suggestions] |

### 5.5 Identified Issues and Priorities

| Issue ID | Description | Severity | Priority | Planned Fix | Owner | Status |
|---|---|---|---|---|---|---|
| BUG-01 | [Issue] | [Severity] | [Priority] | [Fix] | [Name] | [Status] |
| BUG-02 | [Issue] | [Severity] | [Priority] | [Fix] | [Name] | [Status] |

### 5.6 Applied Fixes and Retesting

[Describe the changes made after testing, why they were made, and whether retesting confirmed that the issues were resolved.]

| Issue | Applied Fix | Retest Result | Evidence |
|---|---|---|---|
| [Issue] | [Fix] | [Result] | [Link] |

### 5.7 Playtest Conclusion

[Summarize the overall stability of the playable slice, successful systems, remaining issues, and next actions.]

---

## 6. Performance and Debugging

### 6.1 Performance Profiling

[Describe the profiling tools used, the testing conditions, and the observed performance results.]

| Metric | Observed Value | Notes |
|---|---|---|
| Average FPS | [Value] | [Notes] |
| CPU Frame Time | [Value] | [Notes] |
| GPU Frame Time | [Value] | [Notes] |
| Memory Usage | [Value] | [Notes] |

### 6.2 Bottlenecks and Stability Issues

[Describe any performance bottlenecks, console errors, warnings, or stability problems identified during testing.]

### 6.3 Optimization Actions

[Explain the optimization and debugging actions already completed or planned for Phase 3.]

---

## 7. Production Update

### 7.1 Team Members and Contributions

| Member | Role | Completed Contributions |
|---|---|---|
| [Name] | [Role] | [Contributions] |
| [Name] | [Role] | [Contributions] |
| [Name] | [Role] | [Contributions] |
| [Name] | [Role] | [Contributions] |
| [Name] | [Role] | [Contributions] |

### 7.2 GitHub Project Board

**Project Board Link:** [Link]

[Summarize the current project board status, task distribution, and Phase 2 progress.]

| Status | Number of Tasks |
|---|---|
| Done | [Number] |
| In Progress | [Number] |
| To Do | [Number] |
| Blocked | [Number] |

### 7.3 Completed Tasks

| Task | Owner | GitHub Issue / PR |
|---|---|---|
| [Task] | [Name] | [Link] |
| [Task] | [Name] | [Link] |

### 7.4 Carry-Over Tasks

[Identify incomplete Phase 2 tasks that will continue into Phase 3 and explain why they were carried over.]

| Task | Reason | Owner | Priority |
|---|---|---|---|
| [Task] | [Reason] | [Name] | [Priority] |

### 7.5 Repository and Collaboration Workflow

[Explain how the team uses branches, commits, pull requests, code reviews, issue tracking, and system integration.]

### 7.6 Blockers and Risks

[Describe current technical or production blockers and explain how the team plans to address them.]

---

## 8. Phase 3 Readiness

### 8.1 Systems Ready for Phase 3

- [Completed and tested system]
- [Completed and tested system]
- [Completed and tested system]

### 8.2 Planned Improvements

- Gameplay: [Planned improvement]
- Graphics and Lighting: [Planned improvement]
- Animation and VFX: [Planned improvement]
- UI/UX: [Planned improvement]
- Audio: [Planned improvement]
- Performance: [Planned improvement]
- Testing: [Planned improvement]

### 8.3 Phase 3 Priorities

| Priority | Task | Owner | Expected Completion |
|---|---|---|---|
| High | [Task] | [Name] | [Date] |
| Medium | [Task] | [Name] | [Date] |
| Low | [Task] | [Name] | [Date] |

---

## 9. Competency Coverage

[Explain how the team has applied each course competency during Phase 2 and how it will be further developed during Phase 3.]

| Competency | Current Application | Evidence | Next Action |
|---|---|---|---|
| Core Gameplay Systems Engineering | [Application] | [Link] | [Action] |
| Physics and Collision Systems | [Application] | [Link] | [Action] |
| AI Behavior Design | [Application] | [Link] | [Action] |
| Real-Time Graphics Pipeline | [Application] | [Link] | [Action] |
| Technical Art and Polish | [Application] | [Link] | [Action] |
| Audio Systems Design | [Application] | [Link] | [Action] |
| UI/UX Feedback | [Application] | [Link] | [Action] |
| Performance Optimization and Debugging | [Application] | [Link] | [Action] |
| Production and Collaboration Practices | [Application] | [Link] | [Action] |

---

## 10. Technical Evidence

[Include screenshots, gameplay clips, profiler captures, console logs, and GitHub links that support the implementation and testing claims.]

| Evidence ID | Description | Screenshot / Link |
|---|---|---|
| E01 | Playable Environment | [Evidence] |
| E02 | Player Interaction | [Evidence] |
| E03 | Puzzle Implementation | [Evidence] |
| E04 | Audio System | [Evidence] |
| E05 | Win Condition | [Evidence] |
| E06 | Fail Condition | [Evidence] |
| E07 | Performance Profiler | [Evidence] |
| E08 | GitHub Project Board | [Evidence] |
| E09 | Playtest Evidence | [Evidence] |
| E10 | Pull Requests and Issues | [Evidence] |

---

## 11. Conclusion

[Summarize the current state of the playable vertical slice, the main achievements of Phase 2, the stability of the implemented systems, and the team's readiness for Phase 3.]
