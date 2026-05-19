# A Bumpy Ride - 日本語翻訳 (Mod ABR-jp)

**A Bumpy Ride** の非公式日本語翻訳 Mod です。

---

## 簡単インストール

1. **このフォルダを解凍** します (例: デスクトップに)。
2. `install.ps1` を **右クリック** → **「PowerShellで実行」**。
3. インストーラーは Steam のインストール先を自動検出し、処理を開始します。
   **3~5 分かかります**。
4. 通常通り Steam からゲームを起動してください。

> Windows がスクリプトをブロックする場合、管理者として PowerShell を開いて次を実行:
> ```
> Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned
> ```
> その後、インストーラーを再実行してください。

---

## 手動モード

自動検出に失敗した場合 (Steam が特殊な場所にある、複数のライブラリがあるなど)、
**この `patch-jp` フォルダを直接ゲームフォルダにコピー** できます:

```
F:\Steam\steamapps\common\A Bumpy Ride\patch-jp\install.ps1
```

インストーラーは自分がゲームフォルダ内にいることを自動的に認識し、
パスを尋ねなくなります。

---

## アンインストール

`uninstall.ps1` を右クリック → 「PowerShellで実行」で実行してください。
アンインストーラーは、インストール時に作成された `_ABRjp_backup/`
バックアップから vanilla ファイルを復元します。

バックアップがもう存在しない場合は、Steam から
**ゲームファイルの整合性を確認** することもできます:
*A Bumpy Ride > プロパティ > インストール済みファイル > ゲームファイルの整合性を確認する*。

---

## 動作環境

- **Windows 10 または 11**
- **PowerShell 5.1 以降** (Windows 10/11 にプリインストール)
- **約 12 GB の空き容量** が `%TEMP%` のあるドライブに必要
  (インストール中に一時的に使用、その後解放)
- **A Bumpy Ride を Steam 経由でインストール済み**、オリジナル版で
  (この Mod は vanilla バージョン対応 - Steam がゲームを更新した場合、
  Mod が互換性を失い更新が必要になる可能性があります)

---

## トラブルシューティング

インストーラーは明確なメッセージを表示します。問題が発生した場合:

- **「インストールされているゲームファイルが vanilla と完全に一致しません」**
  → Mod が既にインストールされているか、Steam がゲームを更新した可能性があります。
  まず Steam から **ファイルの整合性を確認** してください。

- **「空き容量が不足しています」** → インストーラーは `%TEMP%` ドライブに
  約 12 GB の空きが必要です。空き容量を確保して再実行してください。

- **「retoc.exe が見つかりません」** → `patch-jp` フォルダが完全に解凍されて
  いません。再度解凍してください。

- **「vanilla 抽出のアセットが少なすぎます」** →
  Steam からゲームの整合性を確認してください。

- **その他のエラー**: エラーメッセージをコピーして Mod の作者に報告してください。

---

## 翻訳されているもの

- すべての **チュートリアル ダイアログ**、クエストの説明、貨物タイプ、
  乗客タイプ。
- **完全な UI** (メニュー、オプション、実績、統計、一日の終わりの画面)。
- **列車とキャラクターのスキンの説明**。
- **建物とクエストのタイプ** (ゲーム内部の enum 経由)。

## 意図的に英語のまま残されているもの

- **固有名詞**: スキン (Comet, Forgotten, Theodore...)、駅
  (Eagle Nest, Seaside, Aurora...)、発見されたエリア
  (Whistling Peaks, Lilli Forest...)。
- 街中の **店の看板** - ウェスタンの雰囲気を保つため。
- オプションの `On` / `Off` ラベル - UI 領域の制約。
- **クレジット** (貢献者と開発チームの名前)。

---

## クレジット

日本語翻訳: **Shayano**
ツール: [retoc-rivals](https://github.com/natimerry/repak-rivals),
[KismetEditor](https://github.com/SolicenTEAM/KismetEditor),
[UAssetAPI](https://github.com/atenfyr/UAssetAPI),
[Dumper-7](https://github.com/Encryqed/Dumper-7).

A Bumpy Ride (C) Choo-Choo Games. この Mod は非公式であり、
ゲーム開発元によって支援または承認されたものではありません。
