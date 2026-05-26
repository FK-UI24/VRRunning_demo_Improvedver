# VRRunningSystem_demo_Improvedver　※制作中

## Overview
This repository provides a demo and improved version of my undergraduate research project.<br>
本リポジトリは、学部研究で開発したVRランニングシステムのデモ版および改良版です。<br>
ユーザーが仮想空間上の走行ルートを自由に作成できるVRトレッドミルシステムを実装しています。<br>
※UnityプロジェクトとFalskサーバーコードは含まれていますが、ビルド済みアプリケーションは含まれていません。<br>

## System Overview
本システムは以下の構成で動作します。<br>
[Sensor] → [Raspberry Pi / Flask] ↔ [Unity] → [VR/Monitor]
- センサー：トレッドミルの速度・傾斜角取得
- Raspberry Pi：取得したデータの計算・処理
- Flask：通信サーバー
- Unity：VR表示・全体制御

## Tech Stack
- Unity(VRアプリケーション開発)
- C#(Unityスクリプト)
- Python(センサーで取得した情報の処理・計算)
- Flask(通信サーバー構築)
- Raspberry Pi 5(ハード制御)
- Meta Quest2(HMD)
- PLATEAU(3D都市モデル)

## What This Repository Contains
- Unityプロジェクト
- Flaskサーバーコード

## Improvements
本デモ版兼改良版では以下を改善しています。<br>
- 仮想環境で使用する3Dモデルの軽量化
- UIの改善
- プログラムの内容と構成の改善・修正

## Limitations
本リポジトリ単体では完全に動作しません。<br>
- トレッドミル実機が必要
- Raspberry Piが必要
- センサー(ロータリーエンコーダー/MPU6050)が必要
- センサー固定具(3Dプリンターで制作したもの)が必要

本リポジトリでは主に、「システムの構造・処理の流れ・実装内容の確認」を目的としてます。

## Original Research
本リポジトリの内容は学部の研究に基づいています。<br>
オリジナル版(詳細な説明など)は以下をご参照ください。<br>
https://github.com/FK-UI24/VRRunning_Original
