# feedback-points.md 対応状況レビュー (2026-03-03)

## 1. Scope

本レビューは、`tasks/feedback-points.md` を一次基準として、各指摘点 (FP1〜FP11) について以下を確認した。

- 現行の権威ドキュメントに明示ルールがあるか
- 関連ドキュメント間で矛盾がないか
- 運用痕跡で裏づけられているか

確認対象:

- `tasks/feedback-points.md`
- `.claude/skills/codex/SKILL.md`
- `.claude/skills/workflow-entry/SKILL.md`
- `.claude/skills/workflow-entry/references/project-manager-guide.md`
- `.claude/skills/workflow-entry/references/sandbox-matrix.md`
- `.claude/skills/workflow-entry/references/runbook.md`
- `.claude/skills/workflow-entry/references/codex-execution-contract.md`
- `.claude/skills/workflow-entry/references/quality-gate-evidence-template.md`
- `tasks/tasks-status.md`
- `tasks/phases-status.md`
- `CLAUDE.md`
- `.claude/skills/**/SKILL.md`
- `reports/` 配下の関連レポート
- `claude-code-workflows/` 配下
- `.git/refs/heads/*`

判定基準:

- `対応済み`: 明示ルールあり、関連文書に矛盾なし、運用痕跡あり
- `部分対応`: 一部反映済みだが、矛盾・抜け・曖昧さ・運用証跡不足が残る
- `未対応`: ルール不在、逆の記述あり、または運用痕跡が明確に不足

結論として、今回の確認範囲では `対応済み` は 0 件、`部分対応` は 8 件、`未対応` は 3 件だった。

## 2. Summary Table

| FP | 要旨 | 判定 | 要約 |
| --- | --- | --- | --- |
| FP1 | 作業前のブランチ作成 | 部分対応 | ルールは明示済みだが、事前作成と強制の運用証跡が不足 |
| FP2 | Codex の direct 実行をデフォルト化 | 未対応 | `feedback-points.md` と `.claude/skills/codex/SKILL.md` が正面衝突 |
| FP3 | `tasks-status` の継続更新 | 部分対応 | ルールはあるが、現状はスナップショットで継続更新の証跡が弱い |
| FP4 | SKILL/関連文書の英語化 | 未対応 | 日本語の権威文書が残っており一次要求を満たしていない |
| FP5 | 批判的レビューと単一責任の徹底 | 部分対応 | 方針はあるが、ワークフロー強制と運用証跡が不足 |
| FP6 | `claude-code-workflows` をベースにする | 未対応 | 一次基準にはあるが、現行契約文書へ昇格していない |
| FP7 | レポートは Codex に作らせる | 部分対応 | 方針はあるが、正式契約化と実運用の裏づけが弱い |
| FP8 | 実装後の独立 Codex レビュー | 部分対応 | 方向性は整合するが、必須フロー化と実績証跡が不足 |
| FP9 | レポート作成は `workspace-write` | 部分対応 | ルールと sandbox 定義は整合するが、実行時証跡がない |
| FP10 | 詳細検証は Codex に委譲 | 部分対応 | 方針はあるが、PM review との役割境界がまだ曖昧 |
| FP11 | PM と Codex の責務分離 | 部分対応 | 上位方針は整合するが、下位運用への徹底と証跡が不足 |

## 3. Detailed Findings

### FP1

判定: `部分対応`

根拠:

- `tasks/feedback-points.md:9,16,17` に「作業前に feature branch を作る」「main へ直接コミットしない」という一次要求がある。
- `.claude/skills/workflow-entry/SKILL.md:113,114` と `.claude/skills/workflow-entry/references/project-manager-guide.md:37,38,40,41` に同趣旨のブランチ運用ルールがある。
- `.claude/skills/workflow-entry/references/runbook.md:68` でも同ルールが再確認されている。
- `.git/refs/heads/master`、`.git/refs/heads/phase-2/task-breakdown`、`.git/refs/heads/phase-3/convergence` から、現在はブランチが存在すること自体は確認できる。
- ただし、「Phase 2 の依頼前にブランチを切った」ことや、「branch 作成が実行時に強制された」ことを示す痕跡はない。

判断理由:

ルールは明示され、文書間の整合も取れているが、運用証跡が「現在 branch がある」以上に進んでいないため `部分対応`。

### FP2

判定: `未対応`

根拠:

- `tasks/feedback-points.md:27,33,38` は「Codex への依頼は direct 実行をデフォルトにする」ことを求めている。
- しかし `.claude/skills/codex/SKILL.md:8,10,42,44` では、tmux 実行がデフォルトであり、direct 実行は明示依頼時のみになっている。

判断理由:

一次要求と現行の権威ドキュメントが直接矛盾しているため `未対応`。

### FP3

判定: `部分対応`

根拠:

- `tasks/feedback-points.md:49,55,58` に `tasks/tasks-status.md` の継続更新要求がある。
- `.claude/skills/workflow-entry/SKILL.md:116,125,129` と `.claude/skills/workflow-entry/references/project-manager-guide.md:19,21` が更新タイミングと完了順序を規定している。
- `CLAUDE.md:5,6` も `tasks-status` / `phases-status` の維持を要求している。
- `tasks/tasks-status.md` は 2026-03-03 更新の完了済みタスク 1 件を示し、`tasks/phases-status.md` は全フェーズ 100% 完了のスナップショットを示している。
- ただし、これらは現時点の状態表示であり、状態変化ごとの継続更新や各マイルストーン時点の更新を示す履歴にはなっていない。

判断理由:

ルールは明示されているが、運用証跡が「継続更新の実施」を十分に証明していないため `部分対応`。

### FP4

判定: `未対応`

根拠:

- `tasks/feedback-points.md:73,79,80` は「SKILL と関連 reference は原則英語」としている。
- `.claude/skills/**/SKILL.md` を横断すると、多くの SKILL 自体は英語化されており、非 entry 系 SKILL にも共通契約が展開されている。
- しかし、権威文書の一部は依然として日本語のままである。代表例は `.claude/skills/workflow-entry/references/codex-execution-contract.md:1` と `CLAUDE.md:1`。

判断理由:

「SKILL と関連文書」の英語化という一次要求に対し、重要な権威文書が未移行のため `未対応`。

### FP5

判定: `部分対応`

根拠:

- `tasks/feedback-points.md:101,106` に、Codex に対して批判的レビューを行い、単一責任を保つ方針がある。
- `.claude/skills/codex/SKILL.md:169,174` は「Codex を権威として盲信せず、必要なら押し返す」ことを明示している。
- `CLAUDE.md:18` も同趣旨を再確認している。
- 一方で、workflow-entry / runbook / execution-contract に、批判的レビューを必須ステップとして強制する実行時ゲートは見当たらない。

判断理由:

方針は文書化されているが、強制力のあるフロー化と実運用証跡が不足するため `部分対応`。

### FP6

判定: `未対応`

根拠:

- `tasks/feedback-points.md:142,150,165` は、新規 workflow を `claude-code-workflows` ベースで作ることを要求している。
- `claude-code-workflows/README.md`、`claude-code-workflows/skills/documentation-criteria/references/design-template.md`、`claude-code-workflows/skills/documentation-criteria/references/plan-template.md`、各 agent spec には再利用可能なベース資産が存在する。
- しかし、この要求は `.claude/skills/workflow-entry/SKILL.md`、`project-manager-guide.md`、`runbook.md`、`codex-execution-contract.md`、`quality-gate-evidence-template.md` には昇格していない。
- `reports/claude-code-workflows-quality-gate-investigation-2026-02-17.md` でも、テンプレート定義と runtime enforcement が分離していることが指摘されている。

判断理由:

一次要求は存在するが、現行の権威ドキュメントに組み込まれておらず、運用強制もないため `未対応`。

### FP7

判定: `部分対応`

根拠:

- `tasks/feedback-points.md:175,192` は「レポートは Codex に作成させ、PM はレビューに徹する」ことを求めている。
- `CLAUDE.md:3,4` は、調査・実装を Codex に委譲し、調査結果を `reports/` に残すことを要求している。
- `reports/` 配下には複数の調査・計画レポートが存在する。
- ただし、workflow-entry / runbook / execution-contract には「レポート作成者は Codex、PM はレビューのみ」という責務分離が明文化されていない。
- 既存レポートも、作成主体が Codex であることを実行時に記録しているわけではない。

判断理由:

方針の方向性はあるが、正式契約化と運用裏づけが不足しているため `部分対応`。

### FP8

判定: `部分対応`

根拠:

- `tasks/feedback-points.md:207,218,220` は、実装後に独立した Codex review を入れるフローを求めている。
- `.claude/skills/codex/SKILL.md:25,57` では、review / diagnose 系の sandbox が read-only start で定義されており、独立レビューの方向性とは整合する。
- しかし、workflow-entry / runbook に「実装後に必ず別フェーズとして独立 Codex review を実行する」明示ルールはない。
- `reports/` 内にも、実装と独立レビューのペア実行を示す継続的な運用証跡は確認できない。

判断理由:

前提となる仕組みは一部整っているが、必須化と運用実績が不足しているため `部分対応`。

### FP9

判定: `部分対応`

根拠:

- `tasks/feedback-points.md:235,242,246` は、レポート作成時に `workspace-write` を使い、read-only は review 用に限定することを求めている。
- `.claude/skills/codex/SKILL.md:22,55` では、doc generation 系に `workspace-write` を割り当てている。
- `.claude/skills/workflow-entry/references/sandbox-matrix.md:13,15` でも design / plan / update-doc が `workspace-write` になっている。
- ただし、既存の `reports/` からは、各レポート作成時に実際どの sandbox が選択されたかを検証できない。

判断理由:

ルール定義と sandbox 方針は整合しているが、実行時証跡がないため `部分対応`。

### FP10

判定: `部分対応`

根拠:

- `tasks/feedback-points.md:255,262,266` は、詳細な検証作業を Codex に委譲し、PM は管理に集中することを求めている。
- `CLAUDE.md:3,4` の委譲方針もこれと整合する。
- 一方で `.claude/skills/workflow-entry/SKILL.md:126` には「manager review and quality check」を含む完了順序があり、PM review が管理レビューなのか、詳細検証も含むのかが明確に切り分けられていない。
- 実行時に「検証主体が Codex だった」ことを記録する運用証跡も見当たらない。

判断理由:

委譲方針はあるが、PM review との責務境界が曖昧で、運用証跡も不足しているため `部分対応`。

### FP11

判定: `部分対応`

根拠:

- `tasks/feedback-points.md:280,297,301` は、PM は管理、Codex は調査・検証・実装・独立レビューという責務分離を求めている。
- `.claude/skills/workflow-entry/references/project-manager-guide.md:5,9` と `.claude/skills/workflow-entry/SKILL.md:110,112` は、PM と実作業側の大枠分担を示している。
- `CLAUDE.md:3,4` も、調査と実装の委譲を明示している。
- ただし、この責務分離は下位の実行契約文書まで完全には落ちておらず、実運用でも一貫して守られたことを示すログはない。

判断理由:

上位方針としては概ね整合しているが、実行契約への展開と運用裏づけが不足しているため `部分対応`。

## 4. Cross-Cutting Issues

### 4.1 Runtime enforcement の不足

多くの項目で、ルール自体は文書にある一方、実際にそのルールが使われたことを示す runtime trace が足りない。特に以下が弱い。

- branch 作成タイミング
- status 更新の履歴性
- review 実施主体
- sandbox 選択結果

`reports/claude-code-workflows-quality-gate-investigation-2026-02-17.md` でも、テンプレート定義と runtime enforcement の分離が指摘されている。

### 4.2 権威文書への昇格不足

FP6 と、FP7〜FP10 の一部は `tasks/feedback-points.md` や `CLAUDE.md` では示されているが、`workflow-entry`、`runbook`、`codex-execution-contract`、`quality-gate-evidence-template` といった実行契約に十分反映されていない。このため、運用ルールというより「方針メモ」に留まっている箇所がある。

### 4.3 未解消の明示矛盾

現在の評価時点で、少なくとも以下の矛盾が残っている。

- FP2: 「direct デフォルト」要求と「tmux デフォルト」実装
- FP4: 「英語化」要求と、日本語の権威文書残存

これらは `部分対応` ではなく、明確に `未対応` を構成する。

### 4.4 スナップショット偏重

`tasks/tasks-status.md` と `tasks/phases-status.md` は現在状態の可視化には有効だが、更新タイミングの妥当性や継続運用を証明する履歴台帳にはなっていない。FP3 の判定を押し上げられない主要因になっている。

## 5. Next Actions

1. FP2 を解消する。`tasks/feedback-points.md` を正とするなら `.claude/skills/codex/SKILL.md` のデフォルトを direct 実行へ変更し、逆に tmux デフォルトを維持するなら一次基準側を修正して矛盾を消す。
2. FP4 を解消する。少なくとも `CLAUDE.md` と `.claude/skills/workflow-entry/references/codex-execution-contract.md` を英語へ統一するか、「英語化対象」の範囲を明示的に狭める。
3. FP6〜FP11 のうち方針止まりの項目を、`workflow-entry`、`runbook`、`codex-execution-contract`、`quality-gate-evidence-template` に昇格する。特に「Codex が作成/検証し、PM は管理レビュー」という責務分離は、実行契約に落とし込むべき。
4. quality gate に runtime trace を追加する。最低でも `branch_name`、`sandbox_mode`、`report_author_role`、`reviewer_role`、`status_files_updated_at` を記録できる形にする。
5. 上記修正後に、同じ判定基準で再監査し、`対応済み` 判定に引き上げられる項目を再評価する。
