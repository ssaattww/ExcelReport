# Feedback Points

プロジェクトマネージャーへの指摘事項を記録

Last Updated: 2026-02-13

## 指摘点リスト

### 指摘点1: ブランチ管理の欠如

**日時**: 2026-02-13
**指摘内容**: Phase 2作業開始前にブランチを分けて作業をしていない

**詳細**:
- workflow-entry/SKILL.md の Project Manager Workflow セクションには明確に記載:
  - "Create a feature branch before starting work"
  - "Direct commits to `main` are prohibited"
  - "For branch rules and lifecycle, see `references/project-manager-guide.md` (`Git Branch Strategy`)"
- Phase 2のタスク洗い出しをcodexに依頼する前に、feature branchを作成すべきだった

**状態**: 未対応（メモのみ）
**優先度**: 高
**対応方針**: 次回以降、作業開始前に必ずブランチ作成を確認する

---

### 指摘点2: Codex実行モードのデフォルト変更

**日時**: 2026-02-13
**指摘内容**: しばらくtmuxは封印したいので、デフォルト動作を直接実行に変更してほしい

**詳細**:
- 現在の codex/SKILL.md はデフォルトがtmux経由の実行
- 直接実行は「user explicitly requests」の場合のみ
- 変更箇所が最小限になるようにSKILL.mdの構成を考慮が必要
- また変わる恐れがあるため、容易に切り替えられる設計が望ましい

**要求事項**:
1. デフォルト動作を「直接実行」に変更
2. 変更箇所を最小限にする設計
3. 後で戻すことも容易な構造にする

**状態**: 未対応（メモのみ）
**優先度**: 高
**対応方針**: SKILL.mdの構造を見直し、実行モードの切り替えを1箇所で制御できるように改善する

---

### 指摘点3: タスク状態管理の更新漏れ

**日時**: 2026-02-13
**指摘内容**: tasks/tasks-status.md の更新を忘れないこと

**詳細**:
- 作業開始時や進捗があった際に tasks-status.md を更新する必要がある
- workflow-entry/SKILL.md の Project Manager Workflow には記載あり:
  - "Maintain a dedicated status file at `tasks/*-status.md` and update it continuously as work progresses"
  - "Completion sequence must follow: 1) codex implementation 2) manager review 3) manager TaskUpdate 4) manager status file update"
- Phase 2作業開始時にtasks-status.mdの更新を失念していた

**メタ指摘**:
- 今後このような指摘を受けたときは、このfeedback-points.mdに追記すること
- 追記することを忘れないように、このファイル自体を定期的に参照すること

**状態**: 未対応（メモのみ）
**優先度**: 高
**対応方針**:
- 作業の各マイルストーンでtasks-status.mdを更新する習慣をつける
- 指摘を受けた際は必ずfeedback-points.mdに記録する

---

### 指摘点4: SKILL系ドキュメントの言語統一

**日時**: 2026-02-13
**指摘内容**: SKILL系の文書は特筆なければ英語で書くこと

**詳細**:
- .claude/skills/*/SKILL.md や関連ドキュメントは英語で記述する
- 日本語で記述する特別な理由がない限り、英語を標準言語とする
- 既存のSKILL.mdは英語で記述されているため、一貫性を保つ必要がある

**影響範囲**:
- 新規作成するSKILL.md
- references/ 配下のドキュメント
- スキル関連のテンプレート
- コミットメッセージやPR説明（SKILLの変更に関するもの）

**状態**: 未対応（メモのみ）
**優先度**: 中
**対応方針**:
- 今後のSKILL関連ドキュメント作成時は英語で記述
- codexへの依頼時に言語指定を明示
- このルールをfeedback-points.mdで管理し、忘れないようにする

---

## 対応履歴

（ここに対応した指摘点の履歴を記録）

