# 🎵 Hướng dẫn sử dụng & kiểm thử

Bản hướng dẫn đi qua toàn bộ tính năng của app theo thứ tự nên chạy. Mỗi mục có phần **kỳ vọng**
để đối chiếu, nên có thể dùng luôn làm checklist kiểm thử sau khi refactor.

Đánh dấu `[x]` vào mục nào đã chạy đúng.

---

## 0. Khởi động

Đóng app nếu đang mở — file `.exe` bị khoá sẽ làm build lỗi. Sau đó:

```bash
dotnet run --project "WpfMusicPlayer/WpfMusicPlayer.csproj"
```

Hoặc mở `WpfMusicPlayer/WpfMusicPlayer.sln` trong Visual Studio và bấm F5.

- [ ] Cửa sổ **Karaoke Player** hiện ra
- [ ] Nếu lần trước đã chọn thư mục nhạc: app tự quét lại, thanh trạng thái hiện `Found N songs`

---

## 1. Thư viện nhạc

| Thao tác | Kỳ vọng |
|---|---|
| Bấm **📁 Scan Music Folder**, chọn thư mục | Trạng thái chạy `Scanning... 12/340`, danh sách đầy dần theo lô chứ không giật từng dòng |
| **Kéo-thả** một file mp3 hoặc cả thư mục vào cửa sổ | `Adding dropped files...` → `Library now has N songs` |
| Gõ vào ô tìm kiếm | Lọc ngay theo tên bài / ca sĩ / album / thể loại |
| Xoá hết chữ trong ô tìm kiếm | Chữ mờ gợi ý hiện lại, **danh sách quay về đầy đủ** |
| Gõ đúng chuỗi `Search songs, artists, albums...` | Vẫn tìm kiếm bình thường (trước đây app sẽ bỏ qua) |

- [ ] Quét thư mục
- [ ] Kéo-thả file / thư mục
- [ ] Tìm kiếm và xoá chữ để quay lại danh sách đầy đủ

---

## 2. Phát nhạc

Double-click một bài để phát, rồi thử:

| Thao tác | Kỳ vọng |
|---|---|
| `Space` | Play / Pause. Nút tròn xanh đổi ▶️ ↔ ⏸️ |
| Gõ chữ có dấu cách trong ô tìm kiếm | Chỉ gõ ra dấu cách, **không** pause nhạc |
| `Ctrl` + `→` / `←` | Bài kế / bài trước |
| Click vào giữa thanh tiến trình | Nhảy đến đúng chỗ vừa click |
| Kéo nút tròn trên thanh tiến trình | Nút đi theo chuột mượt, không giật về; thả ra thì nhạc nhảy tới đó |
| Kéo thanh âm lượng | Âm lượng đổi ngay |
| Để một bài chạy hết | Tự sang bài kế |
| Double-click một bài trong **Queue** | Phát bài đó |
| ❌ trong Queue | Xoá bài khỏi hàng đợi |

Phím tắt đầy đủ:

| Phím | Chức năng |
|---|---|
| `Space` | Play / Pause |
| `Ctrl` + `P` | Play / Pause |
| `Ctrl` + `←` / `→` | Bài trước / bài kế |
| `Ctrl` + `H` | Bật tắt shuffle |
| `Ctrl` + `R` | Đổi chế độ repeat |
| `Ctrl` + `K` | Bật tắt karaoke |
| Phím media trên bàn phím | Play/Pause, Next, Previous |

- [ ] Play / Pause bằng chuột và bằng `Space`
- [ ] `Space` không cướp phím khi đang gõ tìm kiếm
- [ ] Next / Previous
- [ ] Click và kéo thanh tiến trình
- [ ] Âm lượng
- [ ] Tự chuyển bài khi hết

---

## 3. Shuffle và Repeat

> Đây là phần vừa sửa lỗi, nên kiểm kỹ nhất.

Nút 🔀 và 🔁 **sáng xanh** khi bật, xám khi tắt.

### Kịch bản A — shuffle phải đi qua đủ mọi bài

1. Lọc danh sách còn khoảng 5 bài cho dễ đếm.
2. Phát bài đầu, bật 🔀 (hoặc `Ctrl+H`).
3. Bấm ⏭️ liên tục, ghi lại thứ tự.

**Kỳ vọng:** bài đang phát **không bị đổi** khi vừa bật shuffle. Bấm next 4 lần đi qua đúng 4 bài
còn lại, **không lặp bài nào**, lần thứ 5 thì dừng.

> Đây chính là lỗi cũ: trước đây con trỏ shuffle đặt ở đúng vị trí bài hiện tại trong mảng đã xáo,
> nên nếu bài đó rơi vào vị trí thứ 3/5 thì chỉ nghe thêm được 2 bài là hết.

### Kịch bản B — shuffle + repeat all

1. Bật cả 🔀 và 🔁 (bấm 🔁 một lần = Repeat All).
2. Bấm next cho hết vòng.

**Kỳ vọng:** sang vòng mới với thứ tự xáo khác, và **bài đầu vòng mới không phải bài vừa nghe xong**.

### Kịch bản C — repeat one

Bấm 🔁 hai lần (icon thành 🔂), để bài chạy hết.

**Kỳ vọng:** tự phát lại chính nó, và cột **Plays** tăng thêm **mỗi lần lặp**.

- [ ] Kịch bản A
- [ ] Kịch bản B
- [ ] Kịch bản C

---

## 4. Playlist

1. **➕ Create Playlist** → nhập tên, mô tả, click ô vuông để chọn ảnh bìa → OK.
2. Ở danh sách bài, bấm **➕** trên một dòng → chọn playlist vừa tạo.
3. **Double-click** tên playlist ở sidebar → mở cửa sổ chi tiết.

Trong cửa sổ chi tiết:

| Thao tác | Kỳ vọng |
|---|---|
| **➕ Add Songs** | Dialog chọn nhiều bài; ô tìm kiếm trong đó xoá chữ đi thì danh sách quay lại đầy đủ |
| **▶️ Play All** | Phát từ bài đầu playlist |
| Double-click một bài | Phát bài đó |
| 🗑️ trên một dòng | Hỏi xác nhận rồi xoá khỏi playlist |
| **✏️ Edit** | Đổi tên / mô tả; tiêu đề trên header cập nhật ngay |
| Số `N songs` ở header | Tự cập nhật khi thêm / xoá bài |

Ở sidebar: nút 🗑️ cạnh tên playlist xoá cả playlist, có hỏi xác nhận.

- [ ] Tạo playlist kèm ảnh bìa
- [ ] Thêm bài bằng nút ➕
- [ ] Mở chi tiết bằng double-click
- [ ] Add Songs / Play All / xoá bài / Edit
- [ ] Xoá cả playlist

---

## 5. Sửa thông tin bài hát

Bấm ✏️ trên một dòng → đổi Title / Artist / Album / Genre / Year → **Save**.

**Kỳ vọng:** dòng trong danh sách đổi ngay, trạng thái báo `Updated metadata for '...'`, và thông
tin được ghi vào file — mở lại app vẫn còn.

### Điểm quan trọng cần kiểm

Sửa **chỉ mỗi tiêu đề**, không đụng vào ảnh → mở file bằng Windows Explorer xem ảnh bìa.

**Kỳ vọng:** ảnh gốc **còn nguyên độ phân giải cao**. Trước đây thao tác này sẽ ghi đè ảnh gốc
bằng bản thu nhỏ dùng để hiển thị.

Thử tiếp: bấm **Change Image** chọn ảnh mới → Save → ảnh trong file phải là ảnh mới, kích thước gốc.

- [ ] Sửa text và lưu được vào file
- [ ] Sửa text không làm hỏng ảnh bìa gốc
- [ ] Đổi ảnh mới ghi đúng ảnh gốc vào file

---

## 6. Thống kê nghe

Một lượt nghe được tính khi vượt **75%** bài hát.

1. Phát một bài, để chạy quá 3/4.
2. Nhìn cột **Plays** của dòng đó.

**Kỳ vọng:** tăng thêm 1 **ngay lập tức**, không cần bấm gì thêm.

3. Bấm **📊 Listening Statistics** ở sidebar.

**Kỳ vọng:** 3 tab (This Weekend / This Month / All Time), mỗi tab có tổng số lượt, tổng thời gian
và bảng xếp hạng. Nút **Refresh** cập nhật lại.

> Lịch sử nghe cũ sẽ trống. Dữ liệu cũ khoá theo `Song.Id` — vốn được sinh ngẫu nhiên lại sau mỗi
> lần quét — nên các bản ghi đó đã mồ côi từ trước và không cứu được. Đếm lại từ đầu.

- [ ] Cột Plays tăng khi vượt 75%
- [ ] Cửa sổ thống kê hiển thị đủ 3 tab
- [ ] Nút Refresh

---

## 7. Karaoke (AI)

### Kiểm tra trước

Bấm 🎤 khi chưa phát bài nào.

**Kỳ vọng:** hộp thoại *"Play a song first"*. Nếu máy thiếu Python / onnxruntime / ffmpeg thì
thông báo nói **đúng thứ đang thiếu** kèm lệnh cài — không còn im lặng thất bại.

### Tách một bài

1. Phát một bài → nút 🎤 màu **trong suốt** (chưa có stems).
2. Bấm 🎤 hoặc `Ctrl+K`.

**Kỳ vọng:** overlay hiện ra với **phần trăm chạy thật** (5% → 20% → 70% → 100%) và dòng mô tả từ
script Python. Đây là thứ trước đây đứng im suốt cả quá trình.

3. Thử bấm **Cancel** giữa chừng.

**Kỳ vọng:** dừng trong vài giây, tiến trình Python bị kết thúc, trạng thái báo
`Audio separation cancelled`.

4. Chạy lại và để chạy xong.

**Kỳ vọng:** nhạc chuyển sang bản không lời, nút 🎤 chuyển **cam**, panel *Now Playing* hiện dòng
cam `🎤 Karaoke (instrumental)`.

5. Bấm 🎤 lần nữa → quay về bản gốc, **giữ nguyên vị trí đang nghe**.
6. Chuyển sang bài khác rồi quay lại bài vừa tách → nút 🎤 màu **xanh lá**, bấm là chuyển tức thì.

### Ý nghĩa màu nút 🎤

| Màu | Trạng thái |
|---|---|
| Trong suốt | Chưa có stems — bấm sẽ chạy AI, mất vài phút |
| Xanh lá | Đã có stems — chuyển tức thì |
| Cam | Đang ở chế độ karaoke — bấm để về bản gốc |

### Tách hàng loạt

Lọc danh sách còn vài bài → **🎚️ Separate All Shown** → xác nhận.

**Kỳ vọng:** overlay hiển thị `Separating 1/3: <tên bài>`, `2/3`... và **không tự đóng giữa chừng**
sau mỗi bài. Cancel dừng được cả lô.

### Export

Chọn một bài → **💾 Export Stems** → `Yes` = nhạc nền, `No` = giọng hát → chọn nơi lưu.

Stems được lưu ở `%APPDATA%\WpfMusicPlayer\stems\`, **không mất khi Rebuild** nữa.

- [ ] Thông báo dependency chính xác
- [ ] Overlay chạy phần trăm thật
- [ ] Cancel dừng được
- [ ] Chuyển qua lại giữa bản gốc và bản không lời
- [ ] Nút đổi màu đúng theo trạng thái
- [ ] Tách hàng loạt
- [ ] Export nhạc nền và giọng hát

---

## 8. Phát hiện cảm xúc bài hát (AI)

Tính năng đoán "vibe" của bài hát dựa trên đặc trưng âm thanh — **không dùng model huấn luyện**,
nên luôn giải thích được vì sao ra kết quả đó.

### Cách nó nghĩ

Mỗi bài được đặt lên hai trục:

- **valence** — nhạc nghe *tích cực* đến đâu; chủ yếu dựa vào tông trưởng hay tông thứ
- **arousal** — nhạc nghe *kích thích* đến đâu; chủ yếu dựa vào độ to và tempo

Bốn góc phần tư thành bốn cảm xúc:

|  | Tích cực | U tối |
|---|---|---|
| **Năng lượng cao** | 🙂 Happy — tươi sáng, sôi động | ⚡ Energetic — dồn dập, mạnh mẽ |
| **Năng lượng thấp** | 🌙 Calm — ấm áp, thư giãn | 💧 Sad — chậm, trầm lắng |

### Chạy thử

| Thao tác | Kỳ vọng |
|---|---|
| Chọn một bài → **🧠 Analyse Mood** | Overlay chạy vài giây, xong cột **Mood** của dòng đó hiện icon + tên cảm xúc |
| Rê chuột lên ô Mood | Tooltip hiện tông (ví dụ `A minor`), BPM, và hai chỉ số phần trăm |
| Bấm **🧠 Analyse Mood** lại trên chính bài đó | Hiện kết quả **ngay lập tức** — đã cache, không phân tích lại |
| **🧠 Detect All Moods** | Hỏi xác nhận, rồi chạy lần lượt: `Analysing 3/12: <tên bài>` |
| Bấm **Cancel** giữa chừng | Dừng được; các bài đã xong vẫn giữ kết quả |
| **🎭 Mood Playlists** | Tạo tối đa 4 playlist `🙂 Happy`, `⚡ Energetic`, `🌙 Calm`, `💧 Sad` ở sidebar |
| Bấm **🎭 Mood Playlists** lần nữa | Không nhân đôi bài; báo `already up to date` |
| Tắt app rồi mở lại | Cột Mood vẫn còn nguyên, không phải phân tích lại |

### Kiểm tra kết quả có hợp lý không

Thử vài bài bạn biết rõ tính chất:

- Một bài ballad chậm tông thứ → nên ra **💧 Sad**
- Một bài EDM/rock nhanh, to → nên ra **⚡ Energetic** hoặc **🙂 Happy**
- Một bài acoustic nhẹ nhàng tông trưởng → nên ra **🌙 Calm**

> Vì chỉ nghe âm thanh chứ không hiểu lời, một bài lời buồn nhưng phối tươi sáng sẽ bị đoán là
> Happy. Đó là giới hạn của phương pháp, không phải lỗi.

- [ ] Phân tích một bài
- [ ] Tooltip hiện đủ tông / BPM / chỉ số
- [ ] Kết quả được cache, bấm lại ra ngay
- [ ] Phân tích hàng loạt và hủy được
- [ ] Tạo playlist theo cảm xúc
- [ ] Kết quả còn nguyên sau khi khởi động lại

---

## 9. Khôi phục phiên

1. Phát một bài, tua đến giữa, chỉnh âm lượng, bật shuffle.
2. **Đóng app bằng nút X** — đừng kill process, trạng thái được lưu lúc đóng.
3. Mở lại.

**Kỳ vọng:** âm lượng, shuffle / repeat, hàng đợi và bài đang nghe được khôi phục, **thanh tiến
trình dừng đúng chỗ bỏ dở**. Trạng thái báo `Restored your last session - N songs in the queue`.

Nhạc ở trạng thái **tạm dừng**, bấm `Space` để nghe tiếp. Đây là cố ý — mở app lên mà nhạc tự nổ
ra thì khó chịu.

- [ ] Khôi phục âm lượng và chế độ phát
- [ ] Khôi phục hàng đợi
- [ ] Khôi phục đúng vị trí đang nghe

---

## Reset về trắng

Toàn bộ dữ liệu nằm ở một chỗ:

```bash
explorer "%APPDATA%\WpfMusicPlayer"
```

| File / thư mục | Xoá đi thì mất |
|---|---|
| `app_state.json` | Thư mục nhạc đã nhớ, âm lượng, hàng đợi, chế độ phát |
| `playlists.json` + `covers/` | Toàn bộ playlist và ảnh bìa |
| `listening_stats.json` | Lịch sử nghe và số lượt phát |
| `moods.json` | Kết quả phân tích cảm xúc — xoá là phải phân tích lại |
| `stems/` | Kết quả tách nhạc — xoá là phải tách lại từ đầu |

---

## Cài đặt phần AI

Nếu chưa cài, cần đủ ba thứ:

```bash
pip install audio-separator onnxruntime librosa
```

`audio-separator` + `onnxruntime` cho tách giọng, `librosa` cho phát hiện cảm xúc.

```bash
winget install --id Gyan.FFmpeg -e
```

Python phải là **3.12** (bản 3.14 sẽ lỗi khi build `diffq-fixed`), và lệnh `python` phải chạy được
trong terminal — app gọi đúng chữ `python`, không phải `py`.

Kiểm tra:

```bash
python WpfMusicPlayer/debug_audio_separator.py
```

Đủ bốn dấu ✓ là sẵn sàng. Sau khi cài xong phải **khởi động lại app** — Python chỉ được dò một lần
lúc app khởi động.
