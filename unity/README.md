# Unity project

Unity **6000.3.28f1** (Unity 6.3 LTS), with the Unity Test Framework 1.6.0. On bigiron the editor is at `~/Unity/Hub/Editor/6000.3.28f1/Editor/Unity`. Open the same version on any other machine.

```
Assets/Seedfall/Core/                 pure C# game logic         (asmdef: Seedfall.Core)
Assets/Seedfall/Tests/EditMode/       logic tests                (asmdef: Seedfall.Tests.EditMode)
Assets/Seedfall/Tests/PlayMode/       scene-behaviour tests      (asmdef: Seedfall.Tests.PlayMode, empty for now)
```

## Running tests headless

From the repo root:

```bash
~/Unity/Hub/Editor/6000.3.28f1/Editor/Unity -batchmode -nographics -projectPath unity \
  -runTests -testPlatform EditMode \
  -testResults ~/seedfall/logs/test-results-editmode.xml \
  -logFile ~/seedfall/logs/unity-editmode.log
```

- Don't add `-quit`. `-runTests` exits by itself when it's done.
- Results are NUnit XML. The `<test-run>` element carries `result`, `total`, `passed`, `failed` and `skipped`.
- Compile errors show up as `error CS` lines in the log file.
- Paths for `-testResults` and `-logFile` must be absolute.
- The smoke test run takes about 11 s on bigiron.

## License

This uses a Unity Personal license, activated by Hub sign-in on 2026-11-11. The license file says it's valid until **2026-12-11**. Renew it before then by signing in to the Hub again, or batch mode will stop working.
