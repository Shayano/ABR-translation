# A Bumpy Ride - 日本語翻訳 (非公式 Mod)

> 🌍 **他の言語** : 利用可能な翻訳の完全なリストは [README.md](README.md) を参照してください。

[A Bumpy Ride](https://store.steampowered.com/app/2540610/A_Bumpy_Ride/) (Steam の鉄道シミュレーションインディーゲーム) の日本語翻訳 Mod です。

**現在のバージョン : 1.4.7** (2026年5月17日)
**ゲームエンジン : Unreal Engine 5.3.2 (IoStore)**

> 🆕 **v1.4.7** : 日本語翻訳の初公開リリース。`ですます調` 登録、約 900 文字列翻訳。重要な技術修正 : ゲームのビットマップ `UFont` フォント (`Pixel_Times_Font` 等) には CJK グリフが含まれていないため、すべての日本語文字が空のとうふとして表示されていました。Engine 同梱の composite Roboto + DroidSansFallback フォントオーバーライドで修正済 - すべてのひらがな・カタカナ・漢字が正しく表示されます。SpecialPassenger (Actionnaire の 62 タスク目標) と QuestTicket は `BPOffsetPatcher` を使ってクラッシュなしで翻訳されています (FR/DE v1.4.5 と同じ修正)。

> この Mod はゲーム開発元によって開発・支援されているものではありません。ファンプロジェクトとして「現状のまま」提供されます。

---

## 翻訳内容

- すべての UI (メニュー、ボタン、オプション、キーボードショートカット)
- チュートリアルとメインマップのイベントダイアログ (イントロ、通知、ストーリー)
- クエスト、貨物、乗客、建物の説明
- 列車とスキンの説明 (固有名詞は原文のまま)
- 1 日の終わり画面、実績、統計
- 株主タスクの 62 目標 (BPOffsetPatcher 経由)

**意図的に英語のまま** (ゲームの雰囲気に合わせて) :
- 固有名詞 : スキン (Lavish, Stockton, Dayton…)、駅、地域、クレジット
- ピクセルアートの店舗看板 (1900年代ウェスタンの雰囲気)
- `On` / `Off` (UI 一貫性 + ボタン幅の制約)
- 帝国単位 (FT、マイル)
- 略語 `AM` / `PM` (国際的に通用)

**登録** : `ですます調` (標準的な丁寧形、casual gaming 慣例)。

---

## フォントトレードオフ (CJK 制約)

ゲームのオリジナルフォント (`Pixel Times`、`AwfullyDigital`、`Cavalhatriz` のピクセルアート/デジタル風) には CJK グリフが含まれていません。日本語を表示するために、4 つのフォントを Roboto + DroidSansFallback の composite font でオーバーライドしています。

**結果** :
- ✅ すべてのひらがな・カタカナ・漢字が正しく表示
- ❌ Pixel Times / AwfullyDigital の独特な見た目は失われ、UI 全体が Roboto で表示
- ✅ ゲーム内のビットマップフォント (駅の看板、店舗看板用) は変更なし - 元のピクセルアートを保持 (これらは英語のままなので)

---

## インストール

Mod は 2 つの形式で配布されています :
- **Windows インストーラ** (`ABR-jp_v1.4.7.zip`、~30 MB) : Steam を自動検出する PowerShell インストーラ、~3-5 分
- **Drop-in プリパッチ** (`ABR-jp_v1.4.7_prepatched.zip`、~1.9 GB) : container ファイルの直接置き換え、任意の OS (Windows / Linux / Steam Deck / macOS)、インストーラ不要

### 手順 (drop-in プリパッチ)

1. `ABR-jp_v1.4.7_prepatched.zip` をダウンロード ([Releases](../../releases) 参照)
2. ゲームが開いている場合は**閉じる**
3. A Bumpy Ride インストールの `Paks` フォルダを探す :
   - **Windows**   : `<Steam ライブラリ>\steamapps\common\A Bumpy Ride\ABumpyRide\Content\Paks\`
   - **Steam Deck**: `~/.steam/steam/steamapps/common/A Bumpy Ride/ABumpyRide/Content/Paks/`
   - **Linux**     : `~/.local/share/Steam/steamapps/common/A Bumpy Ride/ABumpyRide/Content/Paks/`
4. zip をその `Paks/` フォルダに展開。3 つの既存ファイルが置き換えられます :
   ```
   ABumpyRide-Windows.utoc
   ABumpyRide-Windows.ucas
   ABumpyRide-Windows.pak
   ```
   オリジナルのバックアップは不要 : Steam がいつでも復元できます (アンインストール参照)。
5. Steam から通常通りゲームを起動。メニューが日本語になっているはずです。

### 手順 (Windows インストーラ)

1. `ABR-jp_v1.4.7.zip` をダウンロード
2. ゲームが開いている場合は閉じる
3. zip を任意のフォルダに展開
4. `install.ps1` を実行 (右クリック > PowerShell で実行、~3-5 分)

> 技術メモ : パッチ済みの `.ucas` は ~5.2 GB (vs ~1.6 GB vanilla) になります。ビルドパイプラインが Oodle で再圧縮しないためです。機能的には問題ありません、ディスク容量を多く使うだけです。

---

## アンインストール / 元のバージョンに戻す

手動でバックアップを管理する必要はありません。Steam がワンクリックで vanilla ファイルを復元できます :

1. Steam ライブラリで A Bumpy Ride を**右クリック** → *プロパティ*
2. *インストール済みファイル* → **ゲームファイルの整合性を確認する**
3. Steam が 3 つの変更されたファイルを検出し再ダウンロード (~1.6 GB)
4. 次回起動時、ゲームは元の英語に戻ります

この方法はトラブル時のセーフティネットでもあります : Mod が何かを壊した場合、整合性確認を実行すれば、フォルダを掘り返す必要なくクリーンな状態に戻れます。

---

## 互換性

| 項目 | 状態 |
|---|---|
| ゲームバージョン | 2026 年 5 月 12 日時点の A Bumpy Ride - 最新の Steam アップデートに対応 (Steam app id `2540610`) |
| セーブデータ | 互換性あり、Mod はセーブファイルに触れません |
| マルチプレイヤー | ABR にはマルチプレイヤーなし - 対象外 |
| ゲームアップデート | 公式パッチごとに最新の Mod バージョンを再インストールする必要があります (そうしないとゲームが起動時にクラッシュする可能性あり) |
| FR/DE/ES/JP の共存 | 同時にアクティブにできる `.ucas` container は 1 つだけ - 言語を切り替えるには、片方をアンインストール (Steam 整合性確認) してから他方をインストール |

---

## 既知の問題

- **インストール後にゲームが起動時にクラッシュする** : お使いのゲームのバージョンが Mod がターゲットとしているバージョンよりも新しい可能性があります。Steam で整合性確認を実行して vanilla に戻し、Mod のアップデートを待ってください。
- **一部のテキストが英語のまま** : おそらく意図的に保持された固有名詞 (スキン、駅、地域) です。インターフェイステキストが翻訳されていない場合は、スクリーンショット付きで [issue を開いて](../../issues)ください。
- **奇妙な文字 (とうふ □□□ など)** : フォントオーバーライドが正しくロードされなかった兆候。インストーラを再実行するか、`_ABRjp_backup` から vanilla に戻して再試行してください。問題が続く場合は [issue を開いて](../../issues)ください。
- **QuestBoard と quest ticket の一部の単語が英語のまま** : `Lock` (クエストトレイ上部の鍵ボタン)。これは UMG 内部識別子 (ウィジェットのサブコンポーネント) で、翻訳するとクラッシュを引き起こします。v1.4.7 の既知の制限事項、将来のバージョンで代替アプローチにより修正予定。
- **`AM`/`PM` 2 文字列が英語のまま** : 国際的に通用するため意図的に保持 (例 : `午後 9 時まで乗車を続ける` の隣に英語の `9PM`)。

---

## クレジットと謝辞

- **Mod** : Shayano
- **翻訳** : Claude Code (Anthropic) で生成された AI 支援翻訳。ネイティブスピーカーによる校正はされていません。フィードバックや修正は [GitHub issues](../../issues) で歓迎します。
- **パッチパイプラインに使用したツール** :
  - [retoc-rivals](https://github.com/natimerry/repak-rivals) - IoStore UE5.3 repackager
  - [KissE / KismetEditor](https://github.com/SolicenTEAM/KismetEditor) - Blueprint バイトコードパッチャー
  - [Dumper-7](https://github.com/Encryqed/Dumper-7) - ゲームの `.usmap` 生成
  - [UAssetAPI](https://github.com/atenfyr/UAssetAPI) - UE アセット操作
- **方法論** : Claude Code (Anthropic) とのペアプログラミングで複数のセッションで開発。

---

## ライセンス

この Mod は無料で「現状のまま」提供されます。保証はありません。翻訳されたアセットはオリジナルゲーム (作者の所有物) から派生したものです - 日本語翻訳は個人使用に限り自由に使用できます。

商用再配布は許可されていません。
