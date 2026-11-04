# 從 BaGetter 搬過來

## 和上游的差異

- **.NET 10**，方案檔是 `Buffet.slnx`。
- **只有一個檔案**：`Database:ConnectionString` 是唯一的儲存設定，`Database:Type`、`Storage:*`、`Search:Type` 都沒有了。MySQL、PostgreSQL、SQL Server，以及檔案系統、Azure、AWS、GCP、Aliyun、Tencent 儲存體都已移除。
- **單一檔案上限約 1 GB**（SQLite 的 BLOB 上限），所以 `MaxPackageSizeGiB` 預設是 1。
- **網頁介面**：黑白兩色重新設計；拿掉了上傳頁、統計頁和文件連結，標題和頁尾也不顯示產品名稱與版本。
- **`import files` 指令**：把上游的檔案系統儲存搬進 SQLite。

細節和理由見 [AGENTS.md](../AGENTS.md)。

## 搬移資料

上游預設把檔案放在 `Storage:Path` 底下的 `packages/`、`symbols/` 資料夾。前提是舊的資料庫本來就是 SQLite。把連接字串指向舊資料庫，再對那個資料夾執行一次：

```shell
Database__ConnectionString="Data Source=/data/db/bagetter.db" dotnet Buffet.dll import files /data
```

指令會先套用資料庫移轉，再把資料夾裡的檔案用相同的路徑存進資料庫，重跑也沒關係。同名但內容不同的檔案會算成衝突，此時 exit code 是 1。舊資料夾不會被刪除，確認 Buffet 沒問題之後再自己清掉。
