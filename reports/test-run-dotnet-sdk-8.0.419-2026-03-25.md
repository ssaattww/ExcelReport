# dotnet install + test run report (2026-03-25)

## 実施内容
- `dotnet` 未導入環境に対して `dotnet-install.sh` を利用し、SDK `8.0.419` を `/workspace/.dotnet` に導入。
- `PATH=/workspace/.dotnet:$PATH` を設定して `dotnet --info` を確認。
- `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj` を実行。

## 結果
- テスト結果: **Passed 139 / Failed 0 / Skipped 0**。
- 実行中に既存コード由来の nullable warning は出力されたが、テストは全件成功。

## 実行コマンド
1. `curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh && bash /tmp/dotnet-install.sh --version 8.0.419 --install-dir /workspace/.dotnet`
2. `export PATH=/workspace/.dotnet:$PATH && dotnet --info`
3. `export PATH=/workspace/.dotnet:$PATH && dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj`
