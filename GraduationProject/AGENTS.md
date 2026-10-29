# AI開発・対話ルール (AI Interaction Rules)

## 1. プログラムコードのコメントルール
- **元々書かれていたコメントの維持**:
  - もともとコード内に存在していたコメントやコメントアウトは、勝手に書き換えたり新規に付け加えたりしない。
- **新しく書くコードのコメント**:
  - 新しく書いたプログラム部分にコメントを付けるのはOK。
  - ただし、そこから無駄にコメントを付け足したり増やしたりすることは禁止。
- **コメント内の番号表記の禁止**:
  - コメントを記述する際、「1.」「2.」などの番号（箇条書き数字）は付けない。

## 2. 会話・言語に関するルール
- **完全日本語対応**:
  - 説明、回答、選択肢の提示を含め、会話はすべて日本語で行う。
- **英語の原則禁止**:
  - ユーザーが英語で質問した場合を除き、英語の使用は禁止。
  - どうしても英単語や英語表記を使いたい・使う必要がある場合は、あらかじめユーザーに許可を取ること。
- **英語のままでよいもの（許可済み）**:
  - 画面のボタン名（GitHub などのツールの画面に表示される名前）
  - Unity の機能名
  - プログラムの中の名前（クラス名・変数名・ファイル名など）
  - Unity のアセットについて聞くときの検索語やアセット名（アセットストアの検索語など）

## 3. プログラムの編集に関するルール
- **AIが編集してよいスクリプト**:
  - AIが書いたり修正したりしてよいのは、以下の「※AI担当」と書かれたスクリプトのみ。
    - `Assets/Script/Manager/NegotiationManager.cs`
    - `Assets/Script/Manager/InspectionManager.cs`
    - `Assets/Script/Radio/RadioMusic.cs`
    - `Assets/Script/Key/KeyHintRow.cs`
    - `Assets/Script/Key/KeyHintUI.cs`
    - `Assets/Script/Text/DealResultUI.cs`
    - `Assets/Script/Text/AnimatedMoneyText.cs`
    - `Assets/Script/Inventory/InventoryUI.cs`
    - `Assets/Script/Board/AuctionBoardUI.cs`
    - `Assets/Script/Board/BoardCard.cs`
    - `Assets/Script/Board/ConfirmRow.cs`
    - `Assets/Script/Board/AuctionManager.cs`
  - それ以外のスクリプトは、読んで説明・助言するだけにとどめ、勝手に書き換えない。
- **質問のときは編集しない**:
  - ユーザーがプログラムについて質問しているだけのときは、回答や説明のみ行い、ファイルを勝手に書き換えない。
