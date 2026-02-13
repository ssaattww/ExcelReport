# Logger 詳細設計書 v1

## Status
- As-Is (Planned): 実装クラス/IF は未実装（証跡: `reports/implementation-inventory-2026-02-13.md:35`）。
- To-Be (Planned): 各モジュール横断の進捗/監査記録を実装する（証跡: `reports/issues-and-improvements-2026-02-13.md:98`）。

本書は、Optimizer Excel Report Library における Logger モジュール の詳細設計を示す。
BasicDesign の「進捗・ログ・監査 (Logger)」章で要求されている以下の点を満たす。

レポート進捗粒度（Book → Sheet → Region → CellBatch）

監査情報の格納先（隠しシート）

他モジュール（DslParser / ExpressionEngine / LayoutEngine / WorksheetState / Styles / Renderer）の Issue モデルや責務分担と整合するように設計する。

---

## 1. 概要・位置づけ
### 1.1 モジュール名 / アセンブリ

- モジュール名: Logger
- 想定アセンブリ: ExcelReport.Core（他のコアモジュールと同一）

### 1.2 役割（要約）
Logger は、レポート生成中の以下を一元管理する。
- 進捗:
 - Book 全体 → Sheet → Region（use/repeat/コンポーネント単位）→ CellBatch 単位の進捗通知
- ログ:
  - モジュールごとの情報 / 警告 / エラーの記録（Issue と連携）
- 監査:
  - 生成プロセスの記録を 隠しシート として Excel ファイルに埋め込むためのデータ提供
実装的には、```ReportGenerator``` が作成した ```IReportLogger``` を各モジュールに渡し、各モジュールがそれを通じて進捗・ログを記録する形とする。

---

## 2. 責務・非責務
### 2.1 責務 (IN)
- 進捗通知モデルの定義（Book/Sheet/Region/CellBatch の階層）
- ログエントリモデルの定義（LogLevel/Category/Message/コンテキスト）
- Issue とログの連携（Issue 発生時のログ出力）
- ログ・進捗情報の収集とクエリ API 提供
- 監査シート用データ（AuditLog）モデルの提供

### 2.2 非責務 (OUT)

- Excel 物理出力:
  - 隠しシートの実際の作成・書き込みは Renderer の責務（Logger はシート内容の「元データ」を提供）
- Issue の生成・分類:
  - Issue の生成および IssueKind の定義は各モジュール側（DslParser / WorksheetState 等）の責務
  - Logger は既存 Issue をログとしてミラーするのみ
- 式評価のエラー処理:
  - ExpressionEngine の #ERR(...) 返却などは ExpressionEngine 側の責務。必要に応じて Logger にログを送る。

---

## 3. 進捗モデル
### 3.1 階層構造
BasicDesign の要求に従い、進捗粒度は次の 4 階層とする。

- Book:
  - 1 レポート生成処理全体を 1 Unit とする
  - 例: Book(Templates/Report1.xml)
- Sheet:
  - DSL の <sheet> 要素単位
- Region:
  - シート内の「まとまり」を表す単位
  - 典型例:
    - ```<use>``` インスタンス（component 呼び出し）
    - ```<repeat>``` インスタンス（繰り返し）
    - 手動で区切った大きめの ```<grid>``` 等
- CellBatch:
  - Renderer が ClosedXML 等に対して実際に書き込むセル群のバッチ
  - 単位例:
    - 1 行単位
    - N セル単位のループブロック

### 3.2 進捗モデル型
```csharp
public enum ProgressScope
{
    Book,
    Sheet,
    Region,
    CellBatch
}

public enum ProgressPhase
{
    // 大まかな処理フェーズ
    Preparing,        // テンプレート読み込み・DSL パース
    PlanningLayout,   // LayoutEngine によるレイアウト計画
    BuildingState,    // WorksheetState 構築
    Rendering,        // Renderer による Excel 出力
    Completed,
    Failed
}

/// <summary>
/// 進捗イベント 1 件分のスナップショット。
/// </summary>
public sealed class ProgressEvent
{
    public DateTimeOffset Timestamp { get; init; }

    public ProgressScope Scope { get; init; }
    public ProgressPhase Phase { get; init; }

    /// <summary>Book 単位 ID（レポート単位）。</summary>
    public string BookId { get; init; } = string.Empty;

    /// <summary>対象シート名（Sheet/Region/CellBatch の場合）。</summary>
    public string? SheetName { get; init; }

    /// <summary>Region 名（repeat@name / use@name 等）。</summary>
    public string? RegionName { get; init; }

    /// <summary>進捗の個数情報（0..Total）。未指定の場合もある。</summary>
    public int? Current { get; init; }
    public int? Total { get; init; }

    /// <summary>補助メッセージ。例: "Sheet 'Summary' started".</summary>
    public string? Message { get; init; }
}
```

### 3.3 進捗の通知契約
- Logger は push 型 で進捗イベントを受け取り、
  - 内部バッファに蓄積
  - 任意で IProgress<ProgressEvent> やイベントを通じて外部へ転送
- Renderer は CellBatch 単位で進捗を送信することで、I/O 経過をより細かく報告する。

---

## 4. ログ・監査モデル
### 4.1 ログレベル
```csharp
public enum LogLevel
{
    Trace,
    Debug,
    Info,
    Warning,
    Error,
    Fatal
}
```

- Fatal はレポート継続不能レベル（IssueSeverity.Fatal に対応）
- Warning は処理は継続可能だが注意が必要な状態（scope 違反など）

### 4.2 ログカテゴリ
```csharp
public enum LogCategory
{
    Lifecycle,   // 開始/終了などライフサイクル
    Progress,    // 進捗関連（ProgressEvent と対応）
    Issue,       // Issue に紐づくログ
    Performance, // 所要時間やセル数など
    Diagnostic   // デバッグ情報
}
```

### 4.3 LogEntry
```csharp
public sealed class LogEntry
{
    public DateTimeOffset Timestamp { get; init; }

    public LogLevel Level { get; init; }
    public LogCategory Category { get; init; }

    /// <summary>メッセージ本文（ユーザー向け）。</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>起点モジュール名 (e.g. "DslParser", "ExpressionEngine").</summary>
    public string Module { get; init; } = string.Empty;

    /// <summary>Book 単位 ID。</summary>
    public string BookId { get; init; } = string.Empty;

    /// <summary>対象シート名。</summary>
    public string? SheetName { get; init; }

    /// <summary>Region 名（use@name, repeat@name 等）。</summary>
    public string? RegionName { get; init; }

    /// <summary>セル座標等の詳細（例: "R=10,C=3"）。</summary>
    public string? Location { get; init; }

    /// <summary>紐づく Issue（Issue ログの場合のみ）。</summary>
    public Issue? Issue { get; init; }

    /// <summary>追加プロパティ（キー/値）。</summary>
    public IReadOnlyDictionary<string, string>? Properties { get; init; }
}
```
- Issue 型は DslParser/WorksheetState が利用する共通モデルを参照する。

---

## 5. 公開 API
### 5.1 IReportLogger
```csharp
public interface IReportLogger
{
    // --- 進捗 ---

    /// <summary>
    /// 進捗イベントを記録する。
    /// </summary>
    void ReportProgress(ProgressEvent progress);

    // --- ログ ---

    /// <summary>
    /// 任意のログエントリを記録する（低レベル API）。
    /// </summary>
    void Log(LogEntry entry);

    // よく使うレベル向けヘルパー
    void Info(string message, LogContext? ctx = null);
    void Warn(string message, LogContext? ctx = null);
    void Error(string message, LogContext? ctx = null, Exception? ex = null);

    /// <summary>
    /// Issue をログとして記録する。
    /// </summary>
    void LogIssue(Issue issue, LogContext? ctx = null);

    // --- クエリ ---

    /// <summary>
    /// 現在までに蓄積されたログエントリを取得する。
    /// </summary>
    IReadOnlyList<LogEntry> GetEntries();

    /// <summary>
    /// 現在までに蓄積された進捗イベントを取得する。
    /// </summary>
    IReadOnlyList<ProgressEvent> GetProgressEvents();
}
```
- ```LogContext``` は Book/Sheet/Region/Cell の絞り込み情報をまとめた小さなオブジェクトとする。

```csharp
public sealed class LogContext
{
    public string BookId { get; init; } = string.Empty;
    public string? SheetName { get; init; }
    public string? RegionName { get; init; }
    public string? Location { get; init; } // "R=10,C=3" など
    public string Module { get; init; } = string.Empty;
}
```

### 5.2 IReportLogSink
IReportLogger の実装は、1 つ以上の シンク (sink) に対してログを転送する。
```csharp
public interface IReportLogSink
{
    void OnLog(LogEntry entry);
    void OnProgress(ProgressEvent progress);
}
```

代表的なシンク:
- インメモリ蓄積用シンク（Renderer が監査シートを作るときに利用）
- コンソール/デバッグ出力シンク
- ```IProgress<ProgressEvent>``` 連携シンク（UI 更新など）

### 5.3 監査シート導出 API
監査シート内容は Renderer が Excel に書き込むが、Logger はその元となる行データを提供する。
```csharp
public interface IAuditLogExporter
{
    /// <summary>
    /// 監査シートに出力すべき行データを返す。
    /// </summary>
    IReadOnlyList<AuditRow> ExportAuditRows(
        string bookId,
        IReadOnlyList<LogEntry> entries,
        IReadOnlyList<Issue> issues);
}
```
```AuditRow``` は後述。

---

## 6. 監査情報（隠しシート）仕様
### 6.1 シート名と属性

- シート名: _Audit（先頭アンダースコア＋非表示）
- シート状態:
  - Hidden = true
  - Renderer が Excel 作成時に、通常のシートと合わせて作成する。

### 6.2 行フォーマット
Audit シートは 1 行 = 1 ログ or Issue とするフラットテーブル。
```csharp
public sealed class AuditRow
{
    public DateTimeOffset Timestamp { get; init; }
    public LogLevel Level { get; init; }
    public LogCategory Category { get; init; }

    public string Module { get; init; } = string.Empty;
    public string BookId { get; init; } = string.Empty;
    public string? SheetName { get; init; }
    public string? RegionName { get; init; }
    public string? Location { get; init; }

    public string Message { get; init; } = string.Empty;

    // Issue 関連列
    public IssueSeverity? IssueSeverity { get; init; }
    public IssueKind? IssueKind { get; init; }
    public string? IssueMessage { get; init; }

    // 任意の追加情報を JSON 的にまとめた列
    public string? Extra { get; init; }
}
```
Excel上の列構成（例）:

|Col|名称|内容|
| ---- | ---- | ---- |
|A	|Timestamp	|ISO8601 文字列|
|B	|Level	|```Info``` / ```Warning``` / ```Error``` / ```Fatal``` 等|
|C	|Category	|```Lifecycle``` / ```Issue``` 等|
|D	|Module	|発生モジュール (```DslParser``` 等)|
|E	|BookId	|レポート ID|
|F	|Sheet	|シート名|
|G	|Region	|Region 名|
|H	|Location	|"R=10,C=3" 等|
|I	|Message	|ログ本文|
|J	|IssueSeverity	|Fatal/Error/Warning/Info|
|K	|IssueKind	|```UndefinedStyle``` 等|
|L	|IssueMessage	|Issue.Message|
|M	|Extra	|JSON 形式の追加データ|

### 6.3 Export ポリシー
- 全ての LogEntry + 全ての Issue を行として出力する
  - Issue は LogCategory.Issue としてもログ化されていることが多いが、二重記録を避けるかどうかは実装ポリシーで決定（設計上は「Issue マップを見て、未ログの Issue があれば追加行を生成」のようにしても良い）

- 出力順:
  - ```Timestamp``` 昇順

- シート行 1 行目はヘッダ行（列名）

---

## 7. 内部実装方針
### 7.1 ReportLogger 実装クラス
```csharp
public sealed class ReportLogger : IReportLogger
{
    private readonly List<LogEntry> _entries = new();
    private readonly List<ProgressEvent> _progress = new();
    private readonly List<IReportLogSink> _sinks = new();

    private readonly object _gate = new();

    public ReportLogger(IEnumerable<IReportLogSink>? sinks = null)
    {
        if (sinks != null)
            _sinks.AddRange(sinks);
    }

    public void ReportProgress(ProgressEvent progress)
    {
        lock (_gate)
        {
            _progress.Add(progress);
        }

        foreach (var s in _sinks)
        {
            s.OnProgress(progress);
        }

        // Progress 自体を Info ログとして残したい場合はここで Log() してもよい
    }

    public void Log(LogEntry entry)
    {
        lock (_gate)
        {
            _entries.Add(entry);
        }

        foreach (var s in _sinks)
        {
            s.OnLog(entry);
        }
    }

    public void Info(string message, LogContext? ctx = null)
        => Log(CreateEntry(LogLevel.Info, LogCategory.Lifecycle, message, ctx));

    public void Warn(string message, LogContext? ctx = null)
        => Log(CreateEntry(LogLevel.Warning, LogCategory.Diagnostic, message, ctx));

    public void Error(string message, LogContext? ctx = null, Exception? ex = null)
        => Log(CreateEntry(LogLevel.Error, LogCategory.Diagnostic,
                           ex != null ? $"{message}: {ex.Message}" : message,
                           ctx));

    public void LogIssue(Issue issue, LogContext? ctx = null)
        => Log(CreateIssueEntry(issue, ctx));

    public IReadOnlyList<LogEntry> GetEntries()
    {
        lock (_gate)
        {
            return _entries.ToArray();
        }
    }

    public IReadOnlyList<ProgressEvent> GetProgressEvents()
    {
        lock (_gate)
        {
            return _progress.ToArray();
        }
    }

    // --- private helpers ---

    private static LogEntry CreateEntry(
        LogLevel level,
        LogCategory cat,
        string message,
        LogContext? ctx)
    {
        ctx ??= new LogContext();
        return new LogEntry
        {
            Timestamp = DateTimeOffset.Now,
            Level = level,
            Category = cat,
            Message = message,
            Module = ctx.Module,
            BookId = ctx.BookId,
            SheetName = ctx.SheetName,
            RegionName = ctx.RegionName,
            Location = ctx.Location,
        };
    }

    private static LogEntry CreateIssueEntry(Issue issue, LogContext? ctx)
    {
        ctx ??= new LogContext();

        var level = issue.Severity switch
        {
            IssueSeverity.Fatal   => LogLevel.Fatal,
            IssueSeverity.Error   => LogLevel.Error,
            IssueSeverity.Warning => LogLevel.Warning,
            _                     => LogLevel.Info,
        };

        return new LogEntry
        {
            Timestamp = DateTimeOffset.Now,
            Level = level,
            Category = LogCategory.Issue,
            Message = issue.Message,
            Module = ctx.Module,
            BookId = ctx.BookId,
            SheetName = ctx.SheetName,
            RegionName = ctx.RegionName,
            Location = ctx.Location,
            Issue = issue,
        };
    }
}
```

- ログ・進捗の蓄積とシンクへの配信は スレッドセーフ（_gate ロック）とする。

### 7.2 AuditLogExporter の最小実装
```csharp
public sealed class AuditLogExporter : IAuditLogExporter
{
    public IReadOnlyList<AuditRow> ExportAuditRows(
        string bookId,
        IReadOnlyList<LogEntry> entries,
        IReadOnlyList<Issue> issues)
    {
        var rows = new List<AuditRow>();

        // 1) LogEntry ベース
        foreach (var e in entries.Where(e => e.BookId == bookId))
        {
            rows.Add(new AuditRow
            {
                Timestamp = e.Timestamp,
                Level = e.Level,
                Category = e.Category,
                Module = e.Module,
                BookId = e.BookId,
                SheetName = e.SheetName,
                RegionName = e.RegionName,
                Location = e.Location,
                Message = e.Message,
                IssueSeverity = e.Issue?.Severity,
                IssueKind = e.Issue?.Kind,
                IssueMessage = e.Issue?.Message,
                Extra = null, // 必要なら e.Properties を JSON 化
            });
        }

        // 2) 未ログ Issue を補完する（任意）
        var loggedIssues = new HashSet<Issue>(entries.Where(e => e.Issue != null)
                                                    .Select(e => e.Issue!));
        foreach (var issue in issues)
        {
            if (loggedIssues.Contains(issue))
                continue;

            rows.Add(new AuditRow
            {
                Timestamp = DateTimeOffset.Now,
                Level = IssueSeverityToLogLevel(issue.Severity),
                Category = LogCategory.Issue,
                Module = "Unknown",
                BookId = bookId,
                SheetName = null,
                RegionName = null,
                Location = null,
                Message = issue.Message,
                IssueSeverity = issue.Severity,
                IssueKind = issue.Kind,
                IssueMessage = issue.Message,
            });
        }

        return rows.OrderBy(r => r.Timestamp).ToList();
    }

    private static LogLevel IssueSeverityToLogLevel(IssueSeverity severity)
        => severity switch
        {
            IssueSeverity.Fatal   => LogLevel.Fatal,
            IssueSeverity.Error   => LogLevel.Error,
            IssueSeverity.Warning => LogLevel.Warning,
            _                     => LogLevel.Info,
        };
}
```

---

## 8. 他モジュールとの連携
### 8.1 DslParser

- XML 構文エラー・スキーマ違反・DSL 検証エラーなどで Issue を生成する。
- DslParser は、Issue を返却すると同時に IReportLogger.LogIssue() を呼び出す。
- LogContext.Module を "DslParser" とし、ファイル名/行番号を Location や Issue.Span から補完する。

### 8.2 ExpressionEngine
- 式評価中の例外を #ERR(...) でセルに返すとともに、Logger に Error または Issue として記録する想定。

### 8.3 LayoutEngine
- レイアウト生成の開始/終了を Book/Sheet/Region レベルで ProgressEvent として記録する。

### 8.4 WorksheetState
- セル重複や不正な結合セルなどを Issue として生成し、Logger に渡す。

### 8.5 Styles
- scope 違反等を Warning Issue として生成し、Logger に渡す。

### 8.6 Renderer
- Book/Sheet/CellBatch レベルでの進捗（物理出力）を ReportProgress() で報告。
- Excel 出力終了後、Logger + WorksheetState が保持する Issues を基に:
- ユーザー向け Issues シート（可視）を作成（Renderer の責務）
- AuditLogExporter を用いて _Audit 隠しシートを生成

---

## 9. エラーモデル・性能・テスト観点
### 9.1 エラーモデル

Logger 自身は基本的に Fatal を発生させない。
内部エラー（シンク側の例外など）は握り潰すか、最低限のメモリバッファにのみ記録するポリシーとする。

### 9.2 性能
- 典型的なレポートではログ件数はセル数よりはるかに少ないため、List<T> ベースの保持で十分。
- 高頻度 CellBatch 進捗を記録する場合でも、1 レポートあたり数千件程度を想定。
- ログ追加は O(1)、Audit シート生成時に O(N log N) 程度。

### 9.3 テスト観点
- 進捗:
  - Book/Shet/Region/CellBatch の各レベルで正しく ProgressEvent が出力されること。
- ログ:
  - IssueSeverity に応じて LogLevel が正しくマッピングされること。
  - ```LogContext``` による Book/Sheet/Region/Location が正しく反映されること。
- 監査:
  - AuditRow の列が仕様どおりに埋まること。
  - Renderer 側で ```_Audit``` シートが非表示として作成されること（Renderer テスト）。
- 並列:
  - 複数スレッドからの Log / ReportProgress 呼び出しでも例外が発生しないこと。

---

以上が Logger モジュールの詳細設計である。
