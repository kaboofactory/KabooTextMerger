# Kaboo Text Merger

複数のテキストファイルを、**指定した順番で1つに結合**するWindows向けデスクトップアプリです。  
文字コードや改行コードを明示指定できるので、ログやCSV、メモ断片の統合作業を安定して行えます。

![Kaboo Text Merger Main Window](mainwindow.png)

## 主な機能

- ファイル追加（複数選択）
- フォルダ追加（再帰）
- フォルダ追加時の拡張子フィルタ（任意）
- ドラッグ&ドロップで追加 / 並べ替え
- ファイルごとの入力文字コード指定
- 出力文字コード指定（Shift_JIS / CP932 / UTF-8 BOMあり/なし）
- 出力改行コード指定（CRLF / LF）
- 失敗ファイルの警告表示（部分成功対応）

## 使い方（クイックスタート）

1. `ファイル追加` または `フォルダ追加（再帰）` で入力を集める
2. 必要なら `上へ` / `下へ` またはドラッグで並び順を調整する
3. 出力先、出力文字コード、改行コードを設定する
4. `マージして保存` を押す

## 拡張子フィルタの指定例

`フォルダ追加（再帰）` の横にある `拡張子(任意)` に入力します。

- `.txt,.md`
- `txt;csv`
- `*.log`
- 空欄 / `*` / `*.*`（全件対象）

## 配布物

- マニュアル: [index.html](index.html)
- 配布ZIP（ローカル）: [publish/20260213.zip](publish/20260213.zip)

## 開発者向け

### 前提

- Windows
- .NET SDK（`net10.0-windows` をビルド可能なもの）

### ビルド

```powershell
dotnet build KabooTextMerger/KabooTextMerger.csproj -c Release
```

### 実行

```powershell
dotnet run --project KabooTextMerger/KabooTextMerger.csproj
```

### 発行（例）

```powershell
dotnet publish KabooTextMerger/KabooTextMerger.csproj -c Release -o publish/20260213
```

## プロジェクト構成（主要）

- `KabooTextMerger/` : WPFアプリ本体
- `index.html` : HTMLマニュアル
- `mainwindow.png` : マニュアル/README用スクリーンショット

## ライセンス

本ソフトウェアは **MIT License** のもとで提供します。  
Copyright (c) Kaboo Factory

## 免責事項

本ソフトウェアは「現状のまま」提供され、明示または黙示を問わず、商品性・特定目的適合性・非侵害を含むいかなる保証もありません。  
作者または著作権者は、本ソフトウェアまたはその利用に起因して生じるいかなる請求、損害、その他の責任についても責任を負いません。
