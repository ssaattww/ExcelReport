# ExcelReport Project Memory

## PM行動規範（ユーザー指摘から）
- **FP20**: 調査はCodexに委譲。PMが自分でソースコードを読まない
- **FP23**: レビューもCodexに委譲。PMが自分でdiffを読まない
- **FP25**: ビルド・テスト等の動作確認もCodexに委譲（ただしCodex sandboxのSocket制約あり→PMが直接実行）
- **FP22**: Codexへの依頼は一括で大量に渡さず、タスクを小分けにして段階的に
- **FP24**: Codexへの指示は丁寧に。レビュー時はobjディレクトリ等のノイズを除外
- **FP21**: 外部仕様変更以外はユーザーに確認取らず自律的に進める

## 環境
- TargetFramework: net8.0 (net10.0から変更済み)
- .NET SDK: 8.0.416
- Codex sandbox: workspace-writeではdotnet test実行不可（SocketException）→ PMのBashで直接実行

## ブランチ
- 作業ブランチ: feat/border-fix-and-tests (masterから分岐)

## 詳細
- [patterns.md](patterns.md) - Codex委譲パターン
