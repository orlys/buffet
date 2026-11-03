# AGENTS.md

這是自用的 BaGetter 分支 `main`，從上游 [bagetter/BaGetter](https://github.com/bagetter/BaGetter) 的 `v1.6.5` 分出來。

## 與上游的差異

- **.NET 10**：`global.json` 指定 SDK 10.0（`rollForward: latestMajor`），所有專案都是 `net10.0`，方案檔是 `BaGetter.slnx`。
- **只支援 SQLite**：`Database:Type` 只接受 `Sqlite`，設成其他值會在啟動驗證時失敗。
  - `src/BaGetter.Database.{MySql,PostgreSql,SqlServer}` 還留在原處，方便合併上游，但不在方案裡，`BaGetter.csproj` 也沒有參考它們。
  - AzureTable 資料庫沒有註冊；Azure Blob storage 和 Azure Search 仍然可以用。
  - 不要把 MySQL 加回來，除非 Pomelo 已經發佈支援 EF Core 10 的版本。`Pomelo.EntityFrameworkCore.MySql` 9.0.0 在 EF Core 10 上一呼叫 `UseMySql` 就會丟 `MissingMethodException`。
- **網頁介面**：已移除 Upload 頁和 Statistics 頁（`/upload`、`/stats` 都回 404），也移除了 Documentation 導覽連結和頁面內的上游文件連結；`<title>` 不帶 BaGetter 字樣；也沒有顯示版本的 footer。
- `Statistics:ListConfiguredServices` 仍然保留，用來控制 health check 是否列出使用中的服務。

## 建置與測試

```shell
dotnet build BaGetter.slnx
dotnet test BaGetter.slnx
```

`Dockerfile` 只 restore 和 publish `src/BaGetter`。

## 上傳套件

網頁上沒有上傳頁，請用 NuGet 用戶端推送。service index 是 `https://<host>/v3/index.json`；如果有設 `PathBase`，要接在 host 後面。

```shell
dotnet nuget push -s https://<host>/v3/index.json -k <API-KEY> MyPackage.1.0.0.nupkg
```

- 同一個目錄下如果有對應的 `.snupkg`，`dotnet nuget push` 會一起推送符號套件。
- 新版 NuGet 用戶端拒絕對 `http://` 來源執行 push。要用 `dotnet nuget add source http://<host>/v3/index.json -n bagetter --allow-insecure-connections` 註冊來源（需要帳密時再加 `-u`/`-p`），再用 `-s bagetter` 推送。
- 有設定 `ApiKey` 或 `Authentication:ApiKeys` 時，`-k` 必須相符；兩者都沒設時不驗證。
- 回應碼：`201` 成功；`409` 表示同 id 同版本已經存在（除非開了 `AllowPackageOverwrites`）；`401` 表示 API key 錯誤或是唯讀模式（`IsReadOnlyMode`）；`400` 表示不是有效的套件。
- 有設定 `Authentication:Credentials`（Basic 驗證）時，service index 和符號端點也要帳密，所以要先用帳密註冊來源，再用來源名稱推送（Linux/macOS 要加 `--store-password-in-clear-text`）：

  ```shell
  dotnet nuget add source https://<host>/v3/index.json -n bagetter -u <USER> -p <PASSWORD>
  dotnet nuget push -s bagetter -k <API-KEY> MyPackage.1.0.0.nupkg
  ```

其他用戶端（NuGet CLI、Paket）的寫法見 `readme.md` 的〈Uploading packages〉一節。

## 文件

網頁上已經不連到文件。設定方式（API key、`PathBase`、mirror、storage、health check 等）請看上游文件 <https://www.bagetter.com/docs>，原始檔在本 repo 的 `docs/docs/`。注意：上游文件提到的 MySQL／PostgreSQL／SqlServer／AzureTable 資料庫，以及 Upload、Statistics 頁，在這個分支都不適用。
