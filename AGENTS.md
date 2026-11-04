# AGENTS.md

這是 Buffet，自用的 NuGet 伺服器，repo 在 <https://github.com/orlys/buffet>，分支 `orika/customizations`。它從上游 [bagetter/BaGetter](https://github.com/bagetter/BaGetter) 的 `v1.6.5` 分出來，之後改名為 Buffet。

## 與上游的差異

- **名稱**：專案、namespace、組件都改成 `Buffet.*`（例如 `Buffet.Core`、`BuffetOptions`、`Buffet.dll`），方案檔是 `Buffet.slnx`。
  - 預設資料庫檔是 `buffet.db`，設定根目錄的環境變數是 `BUFFET_CONFIG_ROOT`（上游叫 `BAGET_CONFIG_ROOT`）。
  - 連到上游的網址、說明上游行為的句子（例如「upstream BaGetter 建立的資料庫」），以及 `LICENSE` 裡上游的版權聲明都保留原名，不要改。
  - `docs/`（上游文件網站）、`deployment templates/`、`.github/`、`CODE_OF_CONDUCT.md`、`CONTRIBUTING.md` 是上游留下來的檔案，沒有改名，內容講的是上游。
- **.NET 10**：`global.json` 指定 SDK 10.0（`rollForward: latestMajor`），所有專案都是 `net10.0`。
- **整個 feed 就是一個 SQLite 檔**：目標是輕量簡單，唯一的設定是 `Database:ConnectionString`。
  - 套件的 metadata、搜尋，以及檔案（`.nupkg`、nuspec、readme、icon、`.pdb`）全部放在同一個資料庫。檔案存在 `StorageBlobs` 表，由 `SqliteStorageService` 用 `SqliteBlob` 串流讀寫，資料表由 migration `AddStorageBlobs` 建立。
  - 備份或搬家時，先停止服務再複製這個 `.db` 檔（還有 `-wal`/`-shm` 就一起複製）；不停機就用 `sqlite3 buffet.db ".backup 'backup.db'"`。
  - 已經沒有 `Database:Type`、`Storage:*`、`Search:Type` 這些設定，殘留的值會被忽略。
  - 單一檔案上限約 1 GB（SQLite 的 `SQLITE_MAX_LENGTH`），所以 `MaxPackageSizeGiB` 預設是 1。
  - MySQL、PostgreSQL、SqlServer、AzureTable 這些資料庫，以及檔案系統、Aliyun、AWS、Azure、GCP、Tencent 這些儲存體都已刪除，不要加回來。附帶一提，`Pomelo.EntityFrameworkCore.MySql` 9.0.0 在 EF Core 10 上一呼叫 `UseMySql` 就會丟 `MissingMethodException`。
  - 從上游（檔案系統儲存）搬過來時，服務本身不會讀舊的 `packages/`、`symbols/` 資料夾，要先執行一次 `import files <資料夾>`（`FileStorageImporter`）把檔案搬進資料庫。
- **網頁介面**：已移除 Upload 頁和 Statistics 頁（`/upload`、`/stats` 都回 404），也移除了 Documentation 導覽連結和頁面內的上游文件連結；`<title>` 不帶產品名稱；也沒有顯示版本的 footer。
- **介面樣式**：單色主題（黑白、1.5px 外框、膠囊標籤、粗黑分隔線），色票與字體都在 `src/Buffet.Web/wwwroot/css/site.css` 的 `:root`，深色模式整組反轉。Bootstrap 3 只拿來用 dropdown，它內建的 `.label` class 會蓋掉自訂樣式，所以區段標題用的是 `.section-label`。
- `Statistics:ListConfiguredServices` 仍然保留，用來控制 health check 是否列出使用中的服務。
- `screenshots/` 是 README 用的截圖：本機用 nuget.org 的公開套件起一個 Development 站台，再用 Chrome headless 擷取（寬 1280，`--host-resolver-rules="MAP nuget.example.com 127.0.0.1:<port>"` 讓右上角顯示 `nuget.example.com`，深色版加 `--blink-settings=preferredColorScheme=0`）。

## 建置與測試

```shell
dotnet build Buffet.slnx
dotnet test Buffet.slnx
```

`Dockerfile` 只 restore 和 publish `src/Buffet`。

## 部署（nuget.orika.net）

跑在 homelab 的 K8s：namespace `nuget`，manifest 是 homelab repo 的 `k8s/nuget/bagetter.yaml`；資料是 ms-worker-02 上 hostPath `/data/nuget` 裡的 `db/bagetter.db`。叢集裡的資源名稱、資料庫檔名，以及目前跑的 image `localhost:32000/bagetter:1.6.5-orika.2` 都是改名前的，還叫 bagetter。資料庫路徑是 manifest 明確指定的，所以預設檔名改成 `buffet.db` 不影響它。

建 image 不需要 Docker，用 SDK 直接推到 microk8s 的 registry：

```shell
kubectl -n container-registry port-forward svc/registry 5000:5000   # 另開一個終端
DOTNET_CONTAINER_INSECURE_REGISTRIES=localhost:5000 dotnet publish src/Buffet/Buffet.csproj -c Release -r linux-x64 --self-contained false \
  -t:PublishContainer -p:ContainerRegistry=localhost:5000 -p:ContainerRepository=buffet \
  "-p:ContainerImageTags=\"<版本>;<commit>\"" -p:ContainerBaseImage=mcr.microsoft.com/dotnet/aspnet:10.0 -p:ContainerUser=root
```

節點用 `localhost:32000/buffet:<版本>` 拉 image。改 manifest 的 image 之後 `kubectl apply`；Deployment 是 Recreate，切換約 20 秒。

- `ContainerUser=root`：hostPath 裡的檔案屬於 root，SDK 預設的非 root `app` 使用者寫不了資料庫。
- 資料庫啟動時會切成 WAL（`SqliteContext.RunMigrationsAsync`）。下載會一邊串流 blob 一邊持有讀取鎖，在 rollback journal 模式下，並行 restore 時寫入下載次數會失敗，錯誤是 `database is locked`。所以複製資料庫前要先把 Deployment 縮到 0（image 裡沒有 `sqlite3` 可以線上備份）。
- 從上游 BaGetter（檔案系統儲存）搬過來時，只需要做一次 `dotnet /app/Buffet.dll import files /data`。2026-11-04 已經用改名前的 image 做過：914 個檔案全部匯入，330 個 nupkg 下載回來比對 SHA-256 全部相同。

## 上傳套件

網頁上沒有上傳頁，請用 NuGet 用戶端推送。service index 是 `https://<host>/v3/index.json`；如果有設 `PathBase`，要接在 host 後面。

```shell
dotnet nuget push -s https://<host>/v3/index.json -k <API-KEY> MyPackage.1.0.0.nupkg
```

- 同一個目錄下如果有對應的 `.snupkg`，`dotnet nuget push` 會一起推送符號套件。
- 新版 NuGet 用戶端拒絕對 `http://` 來源執行 push。要用 `dotnet nuget add source http://<host>/v3/index.json -n buffet --allow-insecure-connections` 註冊來源（需要帳密時再加 `-u`/`-p`），再用 `-s buffet` 推送。
- 有設定 `ApiKey` 或 `Authentication:ApiKeys` 時，`-k` 必須相符；兩者都沒設時不驗證。
- 回應碼：`201` 成功；`409` 表示同 id 同版本已經存在（除非開了 `AllowPackageOverwrites`）；`401` 表示 API key 錯誤或是唯讀模式（`IsReadOnlyMode`）；`400` 表示不是有效的套件。
- 有設定 `Authentication:Credentials`（Basic 驗證）時，service index 和符號端點也要帳密，所以要先用帳密註冊來源，再用來源名稱推送（Linux/macOS 要加 `--store-password-in-clear-text`）：

  ```shell
  dotnet nuget add source https://<host>/v3/index.json -n buffet -u <USER> -p <PASSWORD>
  dotnet nuget push -s buffet -k <API-KEY> MyPackage.1.0.0.nupkg
  ```

其他用戶端（NuGet CLI、Paket）的寫法見 `readme.md` 的〈上傳套件〉一節。

## 文件

網頁上已經不連到文件。設定方式（API key、`PathBase`、mirror、health check 等）請看上游文件 <https://www.bagetter.com/docs>，原始檔在本 repo 的 `docs/docs/`。注意：上游文件裡關於資料庫類型、儲存體（檔案系統和各家雲端）、搜尋類型、Upload 頁、Statistics 頁的內容，在 Buffet 都不適用。
