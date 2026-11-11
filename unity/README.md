# Unity project

Not created yet (checklist step 4). Create the Unity project in this folder, so that `unity/Assets`, `unity/Packages` and `unity/ProjectSettings` live here.

Planned structure:

```
Assets/Seedfall/Core/                 pure C# game logic (asmdef: Seedfall.Core)
Assets/Seedfall/Tests/EditMode/       logic tests (asmdef: Seedfall.Tests.EditMode, references Seedfall.Core)
Assets/Seedfall/Tests/PlayMode/       scene-behaviour tests (later)
```

Headless test run (once Unity is installed and licensed):

```
<UnityEditor> -batchmode -nographics -projectPath unity -runTests -testPlatform EditMode -testResults logs/test-results-editmode.xml
```
