# 設定

設定寫在 `appsettings.json`，也可以用環境變數覆寫（`:` 換成 `__`，例如 `Database__ConnectionString`）。

| 設定 | 預設 | 說明 |
|---|---|---|
| `Database:ConnectionString` | `Data Source=buffet.db` | SQLite 檔的位置，整個 feed 都在裡面 |
| `ApiKey` | 空白 | 推送和刪除時要帶的 key；空白表示不驗證 |
| `AllowPackageOverwrites` | `false` | 允許覆蓋同 id、同版本的套件 |
| `PackageDeletionBehavior` | `Unlist` | `Unlist` 取消列出（搜尋不到，但仍可下載），`HardDelete` 直接刪除 |
| `MaxPackageSizeGiB` | `1` | 上傳大小上限 |
| `IsReadOnlyMode` | `false` | 拒絕推送、刪除和重新列出 |
| `PathBase` | 無 | 掛在子路徑下時使用，例如 `/nuget` |
| `Mirror:Enabled` | `false` | 本地沒有的套件向上游（預設 nuget.org）抓取並存下來 |
| `Authentication:Credentials` | 無 | Basic 驗證帳密；設定後連讀取 feed 都要帳密 |
| `Authentication:ApiKeys` | 無 | 多組 API key，和 `ApiKey` 並用 |
| `Retention:MaxMajorVersions` 等 | 無 | 每個套件保留的版本數 |
| `HealthCheck:Path` | `/health` | 健康檢查端點 |

其他選項（例如鏡像的驗證方式）請看上游 BaGetter 的文件。上游文件裡關於資料庫類型、儲存體、搜尋類型、上傳頁和統計頁的內容，在 Buffet 都不適用。
