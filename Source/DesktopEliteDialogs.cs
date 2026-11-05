using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

partial class DesktopApp {
 void OpenRamOptimizerDialog() {
  var dialog = new Window {
   Title = I18n.T("Bộ Tối Ưu RAM Chuyên Sâu & Quản Lý Tiến Trình · PurePC 1.2"),
   Owner = window, Width = 960, Height = 650, MinWidth = 780, MinHeight = 500,
   WindowStartupLocation = WindowStartupLocation.CenterOwner,
   Resources = window.Resources, FontFamily = window.FontFamily, FontSize = 13
  };
  dialog.SetResourceReference(Window.BackgroundProperty, "Canvas");
  dialog.SetResourceReference(Window.ForegroundProperty, "Ink");
  EnableEscapeClose(dialog);

  var root = new DockPanel { Margin = new Thickness(20) };
  dialog.Content = root;

  var header = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
  DockPanel.SetDock(header, Dock.Top);
  root.Children.Add(header);

  header.Children.Add(Label(I18n.T("BỘ TỐI ƯU RAM CHUYÊN SÂU & QUẢN LÝ BỘ NHỚ TIẾN TRÌNH"), 19, true));
  header.Children.Add(Label(I18n.T("Thu dọn bộ nhớ đệm (Working Set) của các tiến trình bằng Win32 SetProcessWorkingSetSize & EmptyWorkingSet.\n★ An toàn 100%: Giải phóng hàng trăm MB đến nhiều GB RAM bị chiếm giữ mà không tắt ứng dụng hay mất dữ liệu."), 12));

  var mem = SystemPerformance.GetMemory();
  var ramBar = new ProgressBar { Height = 8, Margin = new Thickness(0, 6, 0, 6), Maximum = 100, Value = mem.dwMemoryLoad };
  var ramInfo = Label(string.Format("Đang dùng: {0}% ({1} / {2})", mem.dwMemoryLoad, Features.Size((long)(mem.ullTotalPhys - mem.ullAvailPhys)), Features.Size((long)mem.ullTotalPhys)), 13, true);
  var ramBox = new StackPanel { Margin = new Thickness(0, 8, 0, 4) };
  ramBox.Children.Add(ramInfo);
  ramBox.Children.Add(ramBar);
  var ramCard = Card(ramBox);
  ramCard.Padding = new Thickness(14, 10, 14, 10);
  header.Children.Add(ramCard);

  var bottom = new StackPanel { Margin = new Thickness(0, 12, 0, 0) };
  DockPanel.SetDock(bottom, Dock.Bottom);
  root.Children.Add(bottom);

  var statusText = Label(I18n.T("Đang tải danh sách tiến trình tiêu thụ bộ nhớ lớn nhất…"), 12);
  statusText.Margin = new Thickness(0, 0, 0, 6);
  bottom.Children.Add(statusText);

  var btnRow = new WrapPanel();
  bottom.Children.Add(btnRow);

  var grid = new DataGrid { IsReadOnly = true, AutoGenerateColumns = false, RowHeight = Double.NaN, MinRowHeight = 36 };
  root.Children.Add(grid);
  AddChecks(grid, "IsSelected");
  AddColumn(grid, "PID", "Id", 70);
  AddColumn(grid, "TÊN TIẾN TRÌNH", "Name", 180);
  AddColumn(grid, "BỘ NHỚ (WORKING SET)", "WorkingSetLabel", 150);
  AddColumn(grid, "TIÊU ĐỀ CỬA SỔ", "Title", 320);
  AddColumn(grid, "SỐ LUỒNG", "Threads", 90);

  List<ProcessMemoryInfo> procs = new List<ProcessMemoryInfo>();
  Action refreshList = () => {
   procs = ProcessMemoryManager.GetTopProcesses(30);
   grid.ItemsSource = null;
   grid.ItemsSource = procs;
   var curMem = SystemPerformance.GetMemory();
   ramBar.Value = curMem.dwMemoryLoad;
   ramInfo.Text = string.Format("Đang dùng: {0}% ({1} / {2})", curMem.dwMemoryLoad, Features.Size((long)(curMem.ullTotalPhys - curMem.ullAvailPhys)), Features.Size((long)curMem.ullTotalPhys));
   statusText.Text = string.Format("Hiển thị {0} tiến trình hàng đầu. Có thể chọn để dọn riêng lẻ hoặc dọn toàn bộ.", procs.Count);
  };
  refreshList();

  var btnCompactAll = ActionButton(I18n.T("⚡ Thu dọn RAM toàn bộ tiến trình (Deep Compact)"), () => {
   long freedBytes;
   int trimmed = ProcessMemoryManager.OptimizeAll(out freedBytes);
   refreshList();
   try { System.Media.SystemSounds.Asterisk.Play(); } catch {}
   MessageBox.Show(dialog, string.Format("Đã thu dọn bộ nhớ cho {0} tiến trình thành công!\nGiải phóng: {1} RAM vật lý.", trimmed, Features.Size(freedBytes)), "Tối ưu RAM chuyên sâu", MessageBoxButton.OK, MessageBoxImage.Information);
  });
  btnCompactAll.Style = (Style)window.Resources["Primary"];
  btnRow.Children.Add(btnCompactAll);

  btnRow.Children.Add(ActionButton(I18n.T("🧹 Dọn các tiến trình đã chọn"), () => {
   var chosen = procs.Where(x => x.IsSelected).ToList();
   if (chosen.Count == 0) { MessageBox.Show(dialog, "Vui lòng chọn ít nhất 1 tiến trình.", "Thông báo"); return; }
   int count = 0;
   foreach (var p in chosen) {
    if (ProcessMemoryManager.TrimProcess(p.Id)) count++;
   }
   refreshList();
   try { System.Media.SystemSounds.Asterisk.Play(); } catch {}
   MessageBox.Show(dialog, string.Format("Đã thu dọn bộ nhớ cho {0} / {1} tiến trình đã chọn.", count, chosen.Count), "Dọn tiến trình", MessageBoxButton.OK, MessageBoxImage.Information);
  }));

  btnRow.Children.Add(ActionButton(I18n.T("Chọn tất cả"), () => {
   foreach (var p in procs) p.IsSelected = true;
   grid.Items.Refresh();
  }));
  btnRow.Children.Add(ActionButton(I18n.T("Bỏ chọn"), () => {
   foreach (var p in procs) p.IsSelected = false;
   grid.Items.Refresh();
  }));
  btnRow.Children.Add(ActionButton(I18n.T("Làm mới"), refreshList));

  dialog.ShowDialog();
 }

 void OpenDevAiCleanerDialog() {
  var dialog = new Window {
   Title = I18n.T("Dọn dẹp Rác Lập Trình & Mô Hình AI Chuyên Sâu · PurePC 1.2"),
   Owner = window, Width = 980, Height = 650, MinWidth = 780, MinHeight = 500,
   WindowStartupLocation = WindowStartupLocation.CenterOwner,
   Resources = window.Resources, FontFamily = window.FontFamily, FontSize = 13
  };
  dialog.SetResourceReference(Window.BackgroundProperty, "Canvas");
  dialog.SetResourceReference(Window.ForegroundProperty, "Ink");
  EnableEscapeClose(dialog);

  var root = new DockPanel { Margin = new Thickness(20) };
  dialog.Content = root;

  var header = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
  DockPanel.SetDock(header, Dock.Top);
  root.Children.Add(header);

  header.Children.Add(Label(I18n.T("TRÌNH DỌN RÁC LẬP TRÌNH & MÔ HÌNH AI (DEV & AI CLEANER)"), 19, true));
  header.Children.Add(Label(I18n.T("Quét và làm sạch an toàn bộ đệm của các công cụ phát triển phần mềm và AI (NPM, Yarn, pnpm, Python pip, Hugging Face, PyTorch, Rust Cargo, Gradle, Go, Android SDK).\n★ Tiết kiệm từ 5GB đến hơn 40GB dung lượng ổ C:. Các thư mục này sẽ tự tạo lại khi bạn chạy lệnh cài đặt package mới."), 12));

  var bottom = new StackPanel { Margin = new Thickness(0, 12, 0, 0) };
  DockPanel.SetDock(bottom, Dock.Bottom);
  root.Children.Add(bottom);

  var statusText = Label(I18n.T("Đang quét các vị trí bộ đệm lập trình & AI…"), 12);
  statusText.Margin = new Thickness(0, 0, 0, 6);
  bottom.Children.Add(statusText);

  var btnRow = new WrapPanel();
  bottom.Children.Add(btnRow);

  var grid = new DataGrid { IsReadOnly = true, AutoGenerateColumns = false, RowHeight = Double.NaN, MinRowHeight = 36 };
  root.Children.Add(grid);
  AddChecks(grid, "Selected");
  AddColumn(grid, "PHÂN LOẠI", "Category", 180);
  AddColumn(grid, "CÔNG CỤ / NỀN TẢNG", "ToolName", 170);
  AddColumn(grid, "SỐ TỆP", "FileCount", 80);
  AddColumn(grid, "DUNG LƯỢNG RÁC", "SizeLabel", 130);
  AddColumn(grid, "ĐƯỜNG DẪN THƯ MỤC", "Path", 340);

  List<DevAiCacheItem> items = new List<DevAiCacheItem>();
  Action scanCaches = async () => {
   statusText.Text = "Đang rà soát các kho lưu trữ package và model AI…";
   btnRow.IsEnabled = false;
   try {
    items = await Task.Run(() => DevAiCacheCleaner.ScanAll(CancellationToken.None, null));
    grid.ItemsSource = null;
    grid.ItemsSource = items;
    long totalBytes = items.Sum(x => x.Bytes);
    statusText.Text = string.Format("Đã phát hiện {0} kho bộ đệm lập trình & AI. Tổng dung lượng: {1}.", items.Count, Features.Size(totalBytes));
   } catch (Exception ex) {
    statusText.Text = "Lỗi khi quét: " + ex.Message;
   } finally {
    btnRow.IsEnabled = true;
   }
  };
  scanCaches();

  var btnClean = ActionButton(I18n.T("🧹 Dọn sạch mục đã chọn"), async () => {
   var chosen = items.Where(x => x.Selected).ToList();
   if (chosen.Count == 0) { MessageBox.Show(dialog, "Vui lòng chọn ít nhất 1 mục bộ nhớ đệm để dọn.", "Thông báo"); return; }
   long totalBytes = chosen.Sum(x => x.Bytes);
   if (!Confirm(string.Format("Dọn sạch {0} thư mục bộ đệm lập trình/AI ({1})?\n\nToàn bộ tệp sẽ được chuyển vào Thùng rác để bạn có thể khôi phục nếu cần.", chosen.Count, Features.Size(totalBytes)))) return;

   btnRow.IsEnabled = false;
   int done = 0;
   long cleanedBytes = 0;
   foreach (var item in chosen) {
    statusText.Text = "Đang dọn dẹp: " + item.ToolName + "…";
    bool ok = await Task.Run(() => DevAiCacheCleaner.Clean(item, true));
    if (ok) { done++; cleanedBytes += item.Bytes; }
   }
   btnRow.IsEnabled = true;
   try { System.Media.SystemSounds.Asterisk.Play(); } catch {}
   MessageBox.Show(dialog, string.Format("Đã dọn dẹp thành công {0} mục bộ nhớ đệm!\nGiải phóng: {1} an toàn vào Thùng rác.", done, Features.Size(cleanedBytes)), "Hoàn tất", MessageBoxButton.OK, MessageBoxImage.Information);
   scanCaches();
  });
  btnClean.Style = (Style)window.Resources["Primary"];
  btnRow.Children.Add(btnClean);

  btnRow.Children.Add(ActionButton(I18n.T("Chọn tất cả"), () => {
   foreach (var it in items) it.Selected = true;
   grid.Items.Refresh();
  }));
  btnRow.Children.Add(ActionButton(I18n.T("Bỏ chọn"), () => {
   foreach (var it in items) it.Selected = false;
   grid.Items.Refresh();
  }));
  btnRow.Children.Add(ActionButton(I18n.T("Quét lại"), scanCaches));

  dialog.ShowDialog();
 }

 void OpenComponentStoreDialog() {
  var dialog = new Window {
   Title = I18n.T("Phân tích & Tối ưu WinSxS (Windows Component Store) · PurePC 1.2"),
   Owner = window, Width = 880, Height = 600, MinWidth = 720, MinHeight = 460,
   WindowStartupLocation = WindowStartupLocation.CenterOwner,
   Resources = window.Resources, FontFamily = window.FontFamily, FontSize = 13
  };
  dialog.SetResourceReference(Window.BackgroundProperty, "Canvas");
  dialog.SetResourceReference(Window.ForegroundProperty, "Ink");
  EnableEscapeClose(dialog);

  var root = new DockPanel { Margin = new Thickness(20) };
  dialog.Content = root;

  var header = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
  DockPanel.SetDock(header, Dock.Top);
  root.Children.Add(header);

  header.Children.Add(Label(I18n.T("PHÂN TÍCH & TỐI ƯU KHO THÀNH PHẦN WINSXS (DISM)"), 19, true));
  header.Children.Add(Label(I18n.T("Sử dụng công cụ Microsoft DISM chính thức để kiểm tra và thu hồi các gói cập nhật Windows Update lỗi thời.\n★ An toàn tuyệt đối: Không can thiệp thủ công vào file hệ thống; DISM chỉ dọn các bản vá đã được thay thế."), 12));

  var infoGrid = new Grid { Margin = new Thickness(0, 8, 0, 8) };
  infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
  infoGrid.ColumnDefinitions.Add(new ColumnDefinition());
  infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
  infoGrid.ColumnDefinitions.Add(new ColumnDefinition());
  for (int i = 0; i < 2; i++) infoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

  var lblExp = Label("…", 13, true);
  var lblAct = Label("…", 13, true);
  var lblPkg = Label("…", 13, true);
  var lblRec = Label("…", 13, true);

  var k1 = Label("Dung lượng Explorer hiển thị:", 12); k1.SetResourceReference(TextBlock.ForegroundProperty, "Muted");
  var k2 = Label("Dung lượng thực tế WinSxS:", 12); k2.SetResourceReference(TextBlock.ForegroundProperty, "Muted");
  var k3 = Label("Số gói có thể thu hồi:", 12); k3.SetResourceReference(TextBlock.ForegroundProperty, "Muted");
  var k4 = Label("Khuyến nghị dọn dẹp:", 12); k4.SetResourceReference(TextBlock.ForegroundProperty, "Muted");

  Grid.SetRow(k1, 0); Grid.SetColumn(k1, 0); infoGrid.Children.Add(k1);
  Grid.SetRow(lblExp, 0); Grid.SetColumn(lblExp, 1); infoGrid.Children.Add(lblExp);
  Grid.SetRow(k2, 1); Grid.SetColumn(k2, 0); infoGrid.Children.Add(k2);
  Grid.SetRow(lblAct, 1); Grid.SetColumn(lblAct, 1); infoGrid.Children.Add(lblAct);

  Grid.SetRow(k3, 0); Grid.SetColumn(k3, 2); infoGrid.Children.Add(k3);
  Grid.SetRow(lblPkg, 0); Grid.SetColumn(lblPkg, 3); infoGrid.Children.Add(lblPkg);
  Grid.SetRow(k4, 1); Grid.SetColumn(k4, 2); infoGrid.Children.Add(k4);
  Grid.SetRow(lblRec, 1); Grid.SetColumn(lblRec, 3); infoGrid.Children.Add(lblRec);

  var cardInfo = Card(infoGrid);
  cardInfo.Padding = new Thickness(14, 10, 14, 10);
  header.Children.Add(cardInfo);

  var bottom = new StackPanel { Margin = new Thickness(0, 12, 0, 0) };
  DockPanel.SetDock(bottom, Dock.Bottom);
  root.Children.Add(bottom);

  var statusText = Label(I18n.T("Bấm 'Phân tích WinSxS' để kiểm tra kho thành phần Windows qua DISM."), 12);
  statusText.Margin = new Thickness(0, 0, 0, 6);
  bottom.Children.Add(statusText);

  var btnRow = new WrapPanel();
  bottom.Children.Add(btnRow);

  var logBox = new TextBox {
   IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap,
   VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
   FontFamily = new FontFamily("Consolas"), FontSize = 11,
   Margin = new Thickness(0, 4, 0, 0),
   Text = "Nhật ký DISM Console sẵn sàng…"
  };
  logBox.SetResourceReference(Control.BackgroundProperty, "Header");
  logBox.SetResourceReference(Control.ForegroundProperty, "Ink");
  root.Children.Add(logBox);

  var btnAnalyze = ActionButton(I18n.T("🔍 Phân tích WinSxS (Analyze)"), async () => {
   btnRow.IsEnabled = false;
   statusText.Text = "Đang chạy DISM /AnalyzeComponentStore… Vui lòng chờ 1-2 phút…";
   logBox.Text = "Bắt đầu phân tích kho thành phần Windows…\r\n";
   try {
    var res = await ComponentStoreManager.AnalyzeAsync(CancellationToken.None);
    lblExp.Text = res.ExplorerSize;
    lblAct.Text = res.ActualSize;
    lblPkg.Text = res.ReclaimablePackages;
    lblRec.Text = res.CleanupRecommended ? "CÓ (Nên dọn dẹp)" : "KHÔNG (Đã tối ưu)";
    lblRec.Foreground = res.CleanupRecommended ? Brushes.OrangeRed : Brushes.MediumSeaGreen;
    logBox.Text = res.RawOutput;
    statusText.Text = res.CleanupRecommended ? "Phân tích xong: Khuyến nghị dọn dẹp kho thành phần!" : "Phân tích xong: Kho thành phần WinSxS đang trong trạng thái tối ưu.";
    try { System.Media.SystemSounds.Asterisk.Play(); } catch {}
   } catch (Exception ex) {
    statusText.Text = "Lỗi khi phân tích: " + ex.Message;
    logBox.Text += "\r\n[Lỗi] " + ex.Message;
   } finally {
    btnRow.IsEnabled = true;
   }
  });
  btnAnalyze.Style = (Style)window.Resources["Primary"];
  btnRow.Children.Add(btnAnalyze);

  var btnClean = ActionButton(I18n.T("⚡ Dọn dẹp kho thành phần (StartComponentCleanup)"), async () => {
   if (!Confirm("Khởi chạy DISM /StartComponentCleanup để dọn dẹp các bản cập nhật đã thay thế?\nQuá trình có thể mất từ 3 đến 8 phút tùy tốc độ ổ cứng.")) return;
   btnRow.IsEnabled = false;
   statusText.Text = "Đang dọn dẹp WinSxS… Quá trình đang diễn ra ngầm…";
   logBox.Text = "Bắt đầu dọn dẹp WinSxS qua DISM /StartComponentCleanup…\r\n";
   try {
    int code = await ComponentStoreManager.StartCleanupAsync(false, line => {
     window.Dispatcher.Invoke(new Action(() => {
      logBox.AppendText(line + "\r\n");
      logBox.ScrollToEnd();
     }));
    }, CancellationToken.None);
    statusText.Text = string.Format("Dọn dẹp hoàn tất với mã thoát: {0}.", code);
    try { System.Media.SystemSounds.Asterisk.Play(); } catch {}
    MessageBox.Show(dialog, "Đã hoàn tất dọn dẹp kho thành phần Windows WinSxS!", "Hoàn tất", MessageBoxButton.OK, MessageBoxImage.Information);
   } catch (Exception ex) {
    statusText.Text = "Lỗi khi dọn dẹp: " + ex.Message;
    logBox.AppendText("\r\n[Lỗi] " + ex.Message + "\r\n");
   } finally {
    btnRow.IsEnabled = true;
   }
  });
  btnRow.Children.Add(btnClean);

  btnRow.Children.Add(ActionButton(I18n.T("Sao chép nhật ký"), () => {
   try { Clipboard.SetText(logBox.Text); MessageBox.Show(dialog, "Đã sao chép nhật ký vào Clipboard!", "Thông báo"); } catch {}
  }));

  dialog.ShowDialog();
 }

 void OpenGameBoosterDialog() {
  var dialog = new Window {
   Title = I18n.T("Tăng tốc Game & Giảm Độ Trễ Mạng (Low-Latency Booster) · PurePC 1.2"),
   Owner = window, Width = 840, Height = 560, MinWidth = 700, MinHeight = 440,
   WindowStartupLocation = WindowStartupLocation.CenterOwner,
   Resources = window.Resources, FontFamily = window.FontFamily, FontSize = 13
  };
  dialog.SetResourceReference(Window.BackgroundProperty, "Canvas");
  dialog.SetResourceReference(Window.ForegroundProperty, "Ink");
  EnableEscapeClose(dialog);

  var root = new DockPanel { Margin = new Thickness(20) };
  dialog.Content = root;

  var header = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
  DockPanel.SetDock(header, Dock.Top);
  root.Children.Add(header);

  header.Children.Add(Label(I18n.T("TĂNG TỐC GAME & TỐI ƯU HÓA ĐỘ TRỄ MẠNG (LOW-LATENCY BOOSTER)"), 19, true));
  header.Children.Add(Label(I18n.T("Tối ưu độ trễ phản hồi mạng TCP/IP (vô hiệu hóa thuật toán Nagle), tăng mức ưu tiên GPU/CPU cho luồng trò chơi và kích hoạt gói năng lượng Ultimate Performance."), 12));

  var statusBox = new StackPanel();
  var lblTcp = Label("…", 13, true);
  var lblSys = Label("…", 13, true);
  var lblPri = Label("…", 13, true);

  Action updateStatus = () => {
   var st = GameBooster.GetStatus();
   lblTcp.Text = st.IsTcpOptimized ? "✓ Đã tối ưu (TcpAckFrequency=1 & TCPNoDelay=1)" : "● Mặc định Windows (Có độ trễ gom gói tin Nagle)";
   lblTcp.Foreground = st.IsTcpOptimized ? Brushes.MediumSeaGreen : Brushes.Orange;

   lblSys.Text = (st.IsSystemResponsivenessOptimized && st.IsNetworkThrottlingDisabled) ? "✓ Đã tối ưu (SystemResponsiveness=0, Throttling=Tắt)" : "● Mặc định Windows (Dành 20% CPU cho nền, Bật bóp băng thông)";
   lblSys.Foreground = (st.IsSystemResponsivenessOptimized && st.IsNetworkThrottlingDisabled) ? Brushes.MediumSeaGreen : Brushes.Orange;

   lblPri.Text = st.IsGamesPriorityOptimized ? "✓ Đã ưu tiên (GPU Priority 8, Luồng Game: High)" : "● Mặc định Windows (Mức ưu tiên bình thường)";
   lblPri.Foreground = st.IsGamesPriorityOptimized ? Brushes.MediumSeaGreen : Brushes.Orange;
  };
  updateStatus();

  var sGrid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
  sGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(240) });
  sGrid.ColumnDefinitions.Add(new ColumnDefinition());
  for (int i = 0; i < 3; i++) sGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

  var r1 = Label("Độ trễ TCP/IP Card Mạng:", 12); r1.SetResourceReference(TextBlock.ForegroundProperty, "Muted");
  var r2 = Label("Hệ thống & Băng thông Mạng:", 12); r2.SetResourceReference(TextBlock.ForegroundProperty, "Muted");
  var r3 = Label("Mức ưu tiên luồng Game:", 12); r3.SetResourceReference(TextBlock.ForegroundProperty, "Muted");

  Grid.SetRow(r1, 0); Grid.SetColumn(r1, 0); sGrid.Children.Add(r1);
  Grid.SetRow(lblTcp, 0); Grid.SetColumn(lblTcp, 1); sGrid.Children.Add(lblTcp);
  Grid.SetRow(r2, 1); Grid.SetColumn(r2, 0); sGrid.Children.Add(r2);
  Grid.SetRow(lblSys, 1); Grid.SetColumn(lblSys, 1); sGrid.Children.Add(lblSys);
  Grid.SetRow(r3, 2); Grid.SetColumn(r3, 0); sGrid.Children.Add(r3);
  Grid.SetRow(lblPri, 2); Grid.SetColumn(lblPri, 1); sGrid.Children.Add(lblPri);

  statusBox.Children.Add(sGrid);
  var cardStatus = Card(statusBox);
  cardStatus.Padding = new Thickness(14, 10, 14, 10);
  header.Children.Add(cardStatus);

  var bottom = new StackPanel { Margin = new Thickness(0, 12, 0, 0) };
  DockPanel.SetDock(bottom, Dock.Bottom);
  root.Children.Add(bottom);

  var statusText = Label(I18n.T("Chọn 'Kích hoạt Tối ưu Game' để áp dụng bộ tinh chỉnh hiệu năng cao."), 12);
  statusText.Margin = new Thickness(0, 0, 0, 6);
  bottom.Children.Add(statusText);

  var btnRow = new WrapPanel();
  bottom.Children.Add(btnRow);

  var logBox = new TextBox {
   IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap,
   VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
   FontFamily = new FontFamily("Consolas"), FontSize = 11,
   Margin = new Thickness(0, 4, 0, 0),
   Text = "Chi tiết các thiết lập tối ưu hóa độ trễ sẽ hiển thị tại đây…"
  };
  logBox.SetResourceReference(Control.BackgroundProperty, "Header");
  logBox.SetResourceReference(Control.ForegroundProperty, "Ink");
  root.Children.Add(logBox);

  var btnBoost = ActionButton(I18n.T("⚡ Kích hoạt Tối ưu Game (1-Click Boost)"), () => {
   string report;
   bool ok = GameBooster.ApplyOptimizations(out report);
   logBox.Text = report;
   updateStatus();
   try { System.Media.SystemSounds.Asterisk.Play(); } catch {}
   if (ok) {
    statusText.Text = "Đã kích hoạt tối ưu hóa độ trễ mạng và hiệu năng Game thành công!";
    MessageBox.Show(dialog, "Đã kích hoạt bộ tối ưu Game & độ trễ mạng thành công!\nKhởi động lại một số game để nhận hiệu quả cao nhất.", "Hoàn tất", MessageBoxButton.OK, MessageBoxImage.Information);
   } else {
    statusText.Text = "Áp dụng tối ưu có cảnh báo hoặc cần quyền Administrator.";
    MessageBox.Show(dialog, "Không thể ghi một số khóa Registry. Hãy thử mở lại PurePC bằng quyền Run as Administrator.", "Lưu ý", MessageBoxButton.OK, MessageBoxImage.Warning);
   }
  });
  btnBoost.Style = (Style)window.Resources["Primary"];
  btnRow.Children.Add(btnBoost);

  btnRow.Children.Add(ActionButton(I18n.T("🔄 Khôi phục mặc định Windows"), () => {
   if (!Confirm("Khôi phục toàn bộ thiết lập mạng TCP/IP và luồng đa phương tiện về mặc định Windows?")) return;
   string report;
   GameBooster.RestoreDefaults(out report);
   logBox.Text = report;
   updateStatus();
   try { System.Media.SystemSounds.Asterisk.Play(); } catch {}
   statusText.Text = "Đã khôi phục thiết lập mặc định Windows.";
   MessageBox.Show(dialog, "Đã khôi phục thiết lập mạng và Multimedia về mặc định của Windows!", "Khôi phục", MessageBoxButton.OK, MessageBoxImage.Information);
  }));

  dialog.ShowDialog();
 }

 void OpenPrivacyShieldDialog() {
  var dialog = new Window {
   Title = I18n.T("Bảo vệ Quyền Riêng Tư & Tắt Telemetry · PurePC 1.2"),
   Owner = window, Width = 840, Height = 580, MinWidth = 700, MinHeight = 440,
   WindowStartupLocation = WindowStartupLocation.CenterOwner,
   Resources = window.Resources, FontFamily = window.FontFamily, FontSize = 13
  };
  dialog.SetResourceReference(Window.BackgroundProperty, "Canvas");
  dialog.SetResourceReference(Window.ForegroundProperty, "Ink");
  EnableEscapeClose(dialog);

  var root = new DockPanel { Margin = new Thickness(20) };
  dialog.Content = root;

  var header = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
  DockPanel.SetDock(header, Dock.Top);
  root.Children.Add(header);

  header.Children.Add(Label(I18n.T("KHIÊN BẢO VỆ QUYỀN RIÊNG TƯ & TẮT THEO DÕI (PRIVACY SHIELD)"), 19, true));
  header.Children.Add(Label(I18n.T("Vô hiệu hóa toàn bộ cơ chế thu thập dữ liệu chẩn đoán (Diagnostic Data Telemetry), tắt dịch vụ DiagTrack, chặn quảng cáo và tìm kiếm Bing trong Start Menu, đồng thời dọn sạch lịch sử hoạt động và tài liệu mở gần đây."), 12));

  var statusBox = new StackPanel();
  var lblTelem = Label("…", 13, true);
  var lblBing = Label("…", 13, true);
  var lblAd = Label("…", 13, true);
  var lblAct = Label("…", 13, true);

  Action updateStatus = () => {
   var st = PrivacyOptimizer.GetStatus();
   lblTelem.Text = st.IsTelemetryDisabled ? "✓ Đã vô hiệu hóa (Telemetry=0)" : "● Mặc định Windows (Thu thập dữ liệu chẩn đoán)";
   lblTelem.Foreground = st.IsTelemetryDisabled ? Brushes.MediumSeaGreen : Brushes.Orange;

   lblBing.Text = st.IsBingSearchDisabled ? "✓ Đã tắt (Menu Start sạch sẽ, không quảng cáo)" : "● Mặc định Windows (Tìm kiếm web và gợi ý Bing)";
   lblBing.Foreground = st.IsBingSearchDisabled ? Brushes.MediumSeaGreen : Brushes.Orange;

   lblAd.Text = st.IsAdvertisingIdDisabled ? "✓ Đã tắt (Không theo dõi quảng cáo cá nhân)" : "● Mặc định Windows (Bật Advertising ID)";
   lblAd.Foreground = st.IsAdvertisingIdDisabled ? Brushes.MediumSeaGreen : Brushes.Orange;

   lblAct.Text = st.IsActivityHistoryDisabled ? "✓ Đã tắt (Không lưu và tải lên Timeline)" : "● Mặc định Windows (Theo dõi lịch sử ứng dụng)";
   lblAct.Foreground = st.IsActivityHistoryDisabled ? Brushes.MediumSeaGreen : Brushes.Orange;
  };
  updateStatus();

  var sGrid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
  sGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
  sGrid.ColumnDefinitions.Add(new ColumnDefinition());
  for (int i = 0; i < 4; i++) sGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

  var r1 = Label("Thu thập Telemetry Windows:", 12); r1.SetResourceReference(TextBlock.ForegroundProperty, "Muted");
  var r2 = Label("Tìm kiếm Bing Start Menu:", 12); r2.SetResourceReference(TextBlock.ForegroundProperty, "Muted");
  var r3 = Label("Mã nhận dạng quảng cáo:", 12); r3.SetResourceReference(TextBlock.ForegroundProperty, "Muted");
  var r4 = Label("Lịch sử Timeline / Hoạt động:", 12); r4.SetResourceReference(TextBlock.ForegroundProperty, "Muted");

  Grid.SetRow(r1, 0); Grid.SetColumn(r1, 0); sGrid.Children.Add(r1);
  Grid.SetRow(lblTelem, 0); Grid.SetColumn(lblTelem, 1); sGrid.Children.Add(lblTelem);
  Grid.SetRow(r2, 1); Grid.SetColumn(r2, 0); sGrid.Children.Add(r2);
  Grid.SetRow(lblBing, 1); Grid.SetColumn(lblBing, 1); sGrid.Children.Add(lblBing);
  Grid.SetRow(r3, 2); Grid.SetColumn(r3, 0); sGrid.Children.Add(r3);
  Grid.SetRow(lblAd, 2); Grid.SetColumn(lblAd, 1); sGrid.Children.Add(lblAd);
  Grid.SetRow(r4, 3); Grid.SetColumn(r4, 0); sGrid.Children.Add(r4);
  Grid.SetRow(lblAct, 3); Grid.SetColumn(lblAct, 1); sGrid.Children.Add(lblAct);

  statusBox.Children.Add(sGrid);
  var cardStatus = Card(statusBox);
  cardStatus.Padding = new Thickness(14, 10, 14, 10);
  header.Children.Add(cardStatus);

  var bottom = new StackPanel { Margin = new Thickness(0, 12, 0, 0) };
  DockPanel.SetDock(bottom, Dock.Bottom);
  root.Children.Add(bottom);

  var statusText = Label(I18n.T("Chọn 'Kích hoạt Bảo vệ Riêng tư' để tắt toàn bộ cơ chế theo dõi của Windows."), 12);
  statusText.Margin = new Thickness(0, 0, 0, 6);
  bottom.Children.Add(statusText);

  var btnRow = new WrapPanel();
  bottom.Children.Add(btnRow);

  var logBox = new TextBox {
   IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap,
   VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
   FontFamily = new FontFamily("Consolas"), FontSize = 11,
   Margin = new Thickness(0, 4, 0, 0),
   Text = "Chi tiết các thiết lập bảo vệ quyền riêng tư sẽ hiển thị tại đây…"
  };
  logBox.SetResourceReference(Control.BackgroundProperty, "Header");
  logBox.SetResourceReference(Control.ForegroundProperty, "Ink");
  root.Children.Add(logBox);

  var btnHarden = ActionButton(I18n.T("🛡️ Kích hoạt Bảo vệ Riêng tư (Harden Privacy)"), () => {
   string report;
   bool ok = PrivacyOptimizer.ApplyHardening(out report);
   logBox.Text = report;
   updateStatus();
   try { System.Media.SystemSounds.Asterisk.Play(); } catch {}
   if (ok) {
    statusText.Text = "Đã tăng cường bảo vệ quyền riêng tư thành công!";
    MessageBox.Show(dialog, "Đã tắt thành công Telemetry, dịch vụ DiagTrack và tìm kiếm Bing quảng cáo!", "Hoàn tất", MessageBoxButton.OK, MessageBoxImage.Information);
   } else {
    statusText.Text = "Áp dụng có cảnh báo hoặc cần quyền Administrator.";
    MessageBox.Show(dialog, "Một số chính sách Group Policy cần quyền Administrator để áp dụng.", "Lưu ý", MessageBoxButton.OK, MessageBoxImage.Warning);
   }
  });
  btnHarden.Style = (Style)window.Resources["Primary"];
  btnRow.Children.Add(btnHarden);

  btnRow.Children.Add(ActionButton(I18n.T("🧹 Dọn lịch sử Explorer & Run MRU"), () => {
   string report;
   int cleared = PrivacyOptimizer.ClearExplorerMruHistory(out report);
   logBox.Text = report;
   try { System.Media.SystemSounds.Asterisk.Play(); } catch {}
   statusText.Text = string.Format("Đã dọn sạch {0} vị trí lịch sử mở tệp gần đây.", cleared);
   MessageBox.Show(dialog, "Đã dọn sạch lịch sử RecentDocs, Run MRU và thanh địa chỉ Explorer!", "Hoàn tất", MessageBoxButton.OK, MessageBoxImage.Information);
  }));

  btnRow.Children.Add(ActionButton(I18n.T("🔄 Khôi phục mặc định Windows"), () => {
   if (!Confirm("Khôi phục toàn bộ thiết lập quyền riêng tư về mặc định Windows?")) return;
   string report;
   PrivacyOptimizer.RestoreDefaults(out report);
   logBox.Text = report;
   updateStatus();
   try { System.Media.SystemSounds.Asterisk.Play(); } catch {}
   statusText.Text = "Đã khôi phục thiết lập mặc định Windows.";
   MessageBox.Show(dialog, "Đã khôi phục thiết lập quyền riêng tư về mặc định của Windows!", "Khôi phục", MessageBoxButton.OK, MessageBoxImage.Information);
  }));

  dialog.ShowDialog();
 }

 void OpenSystemRepairDialog() {
  var dialog = new Window {
   Title = I18n.T("Sửa Lỗi Tệp Hệ Thống & Phục Hồi Windows (SFC & DISM) · PurePC 1.2"),
   Owner = window, Width = 920, Height = 620, MinWidth = 720, MinHeight = 460,
   WindowStartupLocation = WindowStartupLocation.CenterOwner,
   Resources = window.Resources, FontFamily = window.FontFamily, FontSize = 13
  };
  dialog.SetResourceReference(Window.BackgroundProperty, "Canvas");
  dialog.SetResourceReference(Window.ForegroundProperty, "Ink");
  EnableEscapeClose(dialog);

  var root = new DockPanel { Margin = new Thickness(20) };
  dialog.Content = root;

  var header = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
  DockPanel.SetDock(header, Dock.Top);
  root.Children.Add(header);

  header.Children.Add(Label(I18n.T("SỬA LỖI TỆP HỆ THỐNG & PHỤC HỒI WINDOWS (SFC & DISM)"), 19, true));
  header.Children.Add(Label(I18n.T("Tự động quét, chẩn đoán và thay thế các tệp tin hệ thống Windows bị hỏng hóc hoặc thiếu sót bằng bản sao lưu sạch từ Microsoft Component Store, đồng thời hỗ trợ đặt lại toàn diện ngăn xếp mạng TCP/IP & Winsock."), 12));

  var bottom = new StackPanel { Margin = new Thickness(0, 12, 0, 0) };
  DockPanel.SetDock(bottom, Dock.Bottom);
  root.Children.Add(bottom);

  var statusText = Label(I18n.T("Chọn tác vụ sửa chữa: Quét SFC Scannow, Phục hồi DISM hoặc Đặt lại ngăn xếp mạng."), 12);
  statusText.Margin = new Thickness(0, 0, 0, 6);
  bottom.Children.Add(statusText);

  var btnRow = new WrapPanel();
  bottom.Children.Add(btnRow);

  var logBox = new TextBox {
   IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap,
   VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
   FontFamily = new FontFamily("Consolas"), FontSize = 11,
   Margin = new Thickness(0, 4, 0, 0),
   Text = "Nhật ký sửa chữa hệ thống Windows sẵn sàng..."
  };
  logBox.SetResourceReference(Control.BackgroundProperty, "Header");
  logBox.SetResourceReference(Control.ForegroundProperty, "Ink");
  root.Children.Add(logBox);

  var btnSfc = ActionButton(I18n.T("🛠️ Quét & Sửa tệp hệ thống (SFC Scannow)"), async () => {
   if (!Confirm("Khởi chạy System File Checker (sfc /scannow)?\nQuá trình sẽ kiểm tra toàn bộ tệp DLL/EXE hệ thống và mất từ 3 đến 10 phút.")) return;
   btnRow.IsEnabled = false;
   statusText.Text = "Đang chạy sfc /scannow… Vui lòng không tắt ứng dụng…";
   logBox.Text = "Bắt đầu quét kiểm tra và sửa lỗi tệp hệ thống qua SFC…\r\n";
   try {
    int code = await SystemRepair.RunSfcScanAsync(line => {
     window.Dispatcher.Invoke(new Action(() => {
      logBox.AppendText(line + "\r\n");
      logBox.ScrollToEnd();
     }));
    }, CancellationToken.None);
    statusText.Text = string.Format("Quét SFC kết thúc với mã thoát: {0}.", code);
    try { System.Media.SystemSounds.Asterisk.Play(); } catch {}
    MessageBox.Show(dialog, "Đã hoàn tất quá trình quét sửa lỗi tệp hệ thống SFC!", "Hoàn tất", MessageBoxButton.OK, MessageBoxImage.Information);
   } catch (Exception ex) {
    statusText.Text = "Lỗi khi chạy SFC: " + ex.Message;
    logBox.AppendText("\r\n[Lỗi] " + ex.Message + "\r\n");
   } finally {
    btnRow.IsEnabled = true;
   }
  });
  btnSfc.Style = (Style)window.Resources["Primary"];
  btnRow.Children.Add(btnSfc);

  var btnDism = ActionButton(I18n.T("⚡ Phục hồi kho ảnh Windows (DISM RestoreHealth)"), async () => {
   if (!Confirm("Khởi chạy DISM /Online /Cleanup-Image /RestoreHealth?\nQuá trình sẽ kết nối Windows Update và phục hồi toàn bộ kho ảnh hệ thống.")) return;
   btnRow.IsEnabled = false;
   statusText.Text = "Đang chạy DISM /RestoreHealth… Vui lòng chờ…";
   logBox.Text = "Bắt đầu phục hồi kho ảnh hệ thống Windows qua DISM RestoreHealth…\r\n";
   try {
    int code = await SystemRepair.RunDismRestoreHealthAsync(line => {
     window.Dispatcher.Invoke(new Action(() => {
      logBox.AppendText(line + "\r\n");
      logBox.ScrollToEnd();
     }));
    }, CancellationToken.None);
    statusText.Text = string.Format("DISM RestoreHealth kết thúc với mã thoát: {0}.", code);
    try { System.Media.SystemSounds.Asterisk.Play(); } catch {}
    MessageBox.Show(dialog, "Đã hoàn tất quá trình phục hồi kho ảnh Windows DISM!", "Hoàn tất", MessageBoxButton.OK, MessageBoxImage.Information);
   } catch (Exception ex) {
    statusText.Text = "Lỗi khi chạy DISM: " + ex.Message;
    logBox.AppendText("\r\n[Lỗi] " + ex.Message + "\r\n");
   } finally {
    btnRow.IsEnabled = true;
   }
  });
  btnRow.Children.Add(btnDism);

  btnRow.Children.Add(ActionButton(I18n.T("🌐 Đặt lại mạng TCP/IP & Winsock"), () => {
   if (!Confirm("Khôi phục và thiết lập lại toàn bộ cấu hình mạng Winsock & TCP/IP?\nBạn nên khởi động lại máy tính sau khi hoàn tất.")) return;
   string rep;
   SystemRepair.ResetNetworkStack(out rep);
   logBox.Text = rep;
   try { System.Media.SystemSounds.Asterisk.Play(); } catch {}
   statusText.Text = "Đã thiết lập lại ngăn xếp mạng TCP/IP & Winsock thành công.";
   MessageBox.Show(dialog, "Đã thiết lập lại cấu hình mạng Winsock & TCP/IP thành công!\nHãy khởi động lại máy tính để thay đổi có hiệu lực đầy đủ.", "Hoàn tất", MessageBoxButton.OK, MessageBoxImage.Information);
  }));

  btnRow.Children.Add(ActionButton(I18n.T("Sao chép nhật ký"), () => {
   try { Clipboard.SetText(logBox.Text); MessageBox.Show(dialog, "Đã sao chép nhật ký vào Clipboard!", "Thông báo"); } catch {}
  }));

  dialog.ShowDialog();
 }

 void OpenContextMenuDialog() {
  var dialog = new Window {
   Title = I18n.T("Quản Lý Menu Chuột Phải (Context Menu Manager) · PurePC 1.2"),
   Owner = window, Width = 960, Height = 640, MinWidth = 780, MinHeight = 480,
   WindowStartupLocation = WindowStartupLocation.CenterOwner,
   Resources = window.Resources, FontFamily = window.FontFamily, FontSize = 13
  };
  dialog.SetResourceReference(Window.BackgroundProperty, "Canvas");
  dialog.SetResourceReference(Window.ForegroundProperty, "Ink");
  EnableEscapeClose(dialog);

  var root = new DockPanel { Margin = new Thickness(20) };
  dialog.Content = root;

  var header = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
  DockPanel.SetDock(header, Dock.Top);
  root.Children.Add(header);

  header.Children.Add(Label(I18n.T("QUẢN LÝ MENU CHUỘT PHẢI (SHELL CONTEXT MENU HANDLERS)"), 19, true));
  header.Children.Add(Label(I18n.T("Vô hiệu hóa hoặc bật lại các menu mở rộng bên thứ 3 trong menu chuột phải File Explorer (WinRAR, 7-Zip, Notepad++, Git, VS Code, v.v.).\n★ Giúp menu chuột phải phản hồi tức thì, loại bỏ tình trạng đơ giật khi nhấp chuột phải."), 12));

  var bottom = new StackPanel { Margin = new Thickness(0, 12, 0, 0) };
  DockPanel.SetDock(bottom, Dock.Bottom);
  root.Children.Add(bottom);

  var statusText = Label(I18n.T("Đang tải danh sách menu chuột phải..."), 12);
  statusText.Margin = new Thickness(0, 0, 0, 6);
  bottom.Children.Add(statusText);

  var btnRow = new WrapPanel();
  bottom.Children.Add(btnRow);

  var grid = new DataGrid { IsReadOnly = true, AutoGenerateColumns = false, RowHeight = Double.NaN, MinRowHeight = 36 };
  root.Children.Add(grid);
  AddChecks(grid, "Selected");
  AddColumn(grid, "TÊN MENU / TIỆN ÍCH", "Name", 220);
  AddColumn(grid, "VỊ TRÍ ÁP DỤNG", "Location", 200);
  AddColumn(grid, "TRẠNG THÁI", "StatusLabel", 140);
  AddColumn(grid, "CLSID / GUID", "Clsid", 320);

  List<ContextMenuItem> items = new List<ContextMenuItem>();
  Action loadItems = () => {
   items = ContextMenuManager.Scan();
   grid.ItemsSource = null;
   grid.ItemsSource = items;
   statusText.Text = string.Format("Đã tìm thấy {0} mục menu chuột phải ({1} mục có thể tùy chỉnh).", items.Count, items.Count(x => !x.IsSystemProtected));
  };
  loadItems();

  var btnToggle = ActionButton(I18n.T("⚡ Bật / Tắt mục đã chọn (Toggle)"), () => {
   var chosen = items.Where(x => x.Selected && !x.IsSystemProtected).ToList();
   if (chosen.Count == 0) { MessageBox.Show(dialog, "Vui lòng chọn ít nhất 1 mục không phải hệ thống.", "Thông báo"); return; }
   int count = 0;
   foreach (var item in chosen) {
    string err;
    if (ContextMenuManager.Toggle(item, out err)) count++;
   }
   loadItems();
   try { System.Media.SystemSounds.Asterisk.Play(); } catch {}
   MessageBox.Show(dialog, string.Format("Đã cập nhật trạng thái cho {0} mục menu chuột phải!", count), "Hoàn tất", MessageBoxButton.OK, MessageBoxImage.Information);
  });
  btnToggle.Style = (Style)window.Resources["Primary"];
  btnRow.Children.Add(btnToggle);

  btnRow.Children.Add(ActionButton(I18n.T("Chọn tất cả có thể tắt"), () => {
   foreach (var it in items) it.Selected = !it.IsSystemProtected && it.IsEnabled;
   grid.Items.Refresh();
  }));
  btnRow.Children.Add(ActionButton(I18n.T("Bỏ chọn"), () => {
   foreach (var it in items) it.Selected = false;
   grid.Items.Refresh();
  }));
  btnRow.Children.Add(ActionButton(I18n.T("Làm mới"), loadItems));

  dialog.ShowDialog();
 }

 void OpenSsdTrimDialog() {
  var dialog = new Window {
   Title = I18n.T("Tối Ưu Hóa & Kích Hoạt TRIM SSD · PurePC 1.2"),
   Owner = window, Width = 840, Height = 580, MinWidth = 700, MinHeight = 440,
   WindowStartupLocation = WindowStartupLocation.CenterOwner,
   Resources = window.Resources, FontFamily = window.FontFamily, FontSize = 13
  };
  dialog.SetResourceReference(Window.BackgroundProperty, "Canvas");
  dialog.SetResourceReference(Window.ForegroundProperty, "Ink");
  EnableEscapeClose(dialog);

  var root = new DockPanel { Margin = new Thickness(20) };
  dialog.Content = root;

  var header = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
  DockPanel.SetDock(header, Dock.Top);
  root.Children.Add(header);

  header.Children.Add(Label(I18n.T("TỐI ƯU HÓA TUỔI THỌ & KÍCH HOẠT TRIM SSD (SOLID STATE DRIVE)"), 19, true));
  header.Children.Add(Label(I18n.T("Lệnh TRIM thông báo cho chip điều khiển SSD biết các khối dữ liệu (LBA) không còn sử dụng để thu dọn trước, giúp duy trì tốc độ đọc ghi tối đa và kéo dài tuổi thọ chip nhớ NAND Flash."), 12));

  var statusBox = new StackPanel();
  var lblTrim = Label("...", 13, true);
  var lblReFs = Label("...", 13, true);

  Action checkTrim = () => {
   var st = SsdTrimOptimizer.QueryTrimStatus();
   lblTrim.Text = st.StatusText;
   lblTrim.Foreground = st.IsTrimOptimal ? Brushes.MediumSeaGreen : Brushes.OrangeRed;
   lblReFs.Text = st.IsReFsTrimEnabled ? "✓ ReFS TRIM Bật" : "● ReFS TRIM Mặc định";
  };
  checkTrim();

  var sGrid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
  sGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
  sGrid.ColumnDefinitions.Add(new ColumnDefinition());
  sGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
  sGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

  var r1 = Label("Trạng thái TRIM (NTFS):", 12); r1.SetResourceReference(TextBlock.ForegroundProperty, "Muted");
  var r2 = Label("Trạng thái TRIM (ReFS):", 12); r2.SetResourceReference(TextBlock.ForegroundProperty, "Muted");

  Grid.SetRow(r1, 0); Grid.SetColumn(r1, 0); sGrid.Children.Add(r1);
  Grid.SetRow(lblTrim, 0); Grid.SetColumn(lblTrim, 1); sGrid.Children.Add(lblTrim);
  Grid.SetRow(r2, 1); Grid.SetColumn(r2, 0); sGrid.Children.Add(r2);
  Grid.SetRow(lblReFs, 1); Grid.SetColumn(lblReFs, 1); sGrid.Children.Add(lblReFs);

  statusBox.Children.Add(sGrid);
  var cardStatus = Card(statusBox);
  cardStatus.Padding = new Thickness(14, 10, 14, 10);
  header.Children.Add(cardStatus);

  var bottom = new StackPanel { Margin = new Thickness(0, 12, 0, 0) };
  DockPanel.SetDock(bottom, Dock.Bottom);
  root.Children.Add(bottom);

  var statusText = Label(I18n.T("Sẵn sàng kiểm tra hoặc Re-TRIM ổ cứng SSD."), 12);
  statusText.Margin = new Thickness(0, 0, 0, 6);
  bottom.Children.Add(statusText);

  var btnRow = new WrapPanel();
  bottom.Children.Add(btnRow);

  var logBox = new TextBox {
   IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap,
   VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
   FontFamily = new FontFamily("Consolas"), FontSize = 11,
   Margin = new Thickness(0, 4, 0, 0),
   Text = "Nhật ký kiểm tra TRIM SSD sẵn sàng..."
  };
  logBox.SetResourceReference(Control.BackgroundProperty, "Header");
  logBox.SetResourceReference(Control.ForegroundProperty, "Ink");
  root.Children.Add(logBox);

  var btnEnable = ActionButton(I18n.T("⚡ Kích hoạt TRIM (Enable TRIM)"), () => {
   string report;
   bool ok = SsdTrimOptimizer.EnableTrim(out report);
   logBox.Text = report;
   checkTrim();
   try { System.Media.SystemSounds.Asterisk.Play(); } catch {}
   if (ok) {
    statusText.Text = "Đã kích hoạt tính năng TRIM cho SSD thành công!";
    MessageBox.Show(dialog, "Đã bật lệnh TRIM trong Windows!", "Hoàn tất", MessageBoxButton.OK, MessageBoxImage.Information);
   } else {
    MessageBox.Show(dialog, "Cần quyền Administrator để thay đổi cấu hình fsutil.", "Lưu ý", MessageBoxButton.OK, MessageBoxImage.Warning);
   }
  });
  btnEnable.Style = (Style)window.Resources["Primary"];
  btnRow.Children.Add(btnEnable);

  var btnRetrim = ActionButton(I18n.T("🚀 Chạy Re-TRIM ổ C: (defrag /L)"), async () => {
   btnRow.IsEnabled = false;
   statusText.Text = "Đang phát lệnh Re-TRIM cho ổ C:...";
   logBox.Text = "Khởi chạy defrag.exe C: /L (Re-TRIM SSD)...\r\n";
   try {
    int code = await SsdTrimOptimizer.RunRetrimAsync("C", line => {
     window.Dispatcher.Invoke(new Action(() => {
      logBox.AppendText(line + "\r\n");
      logBox.ScrollToEnd();
     }));
    }, CancellationToken.None);
    statusText.Text = string.Format("Re-TRIM kết thúc với mã: {0}.", code);
    try { System.Media.SystemSounds.Asterisk.Play(); } catch {}
    MessageBox.Show(dialog, "Đã gửi tín hiệu Re-TRIM cho chip điều khiển SSD thành công!", "Hoàn tất", MessageBoxButton.OK, MessageBoxImage.Information);
   } catch (Exception ex) {
    statusText.Text = "Lỗi khi Re-TRIM: " + ex.Message;
    logBox.AppendText("\r\n[Lỗi] " + ex.Message + "\r\n");
   } finally {
    btnRow.IsEnabled = true;
   }
  });
  btnRow.Children.Add(btnRetrim);

  btnRow.Children.Add(ActionButton(I18n.T("Kiểm tra lại"), () => { checkTrim(); statusText.Text = "Đã kiểm tra lại trạng thái TRIM."; }));

  dialog.ShowDialog();
 }

 void OpenFolderSpaceDialog() {
  var dialog = new Window {
   Title = I18n.T("Phân Tích Trực Quan Dung Lượng Thư Mục · PurePC 1.2"),
   Owner = window, Width = 960, Height = 640, MinWidth = 780, MinHeight = 480,
   WindowStartupLocation = WindowStartupLocation.CenterOwner,
   Resources = window.Resources, FontFamily = window.FontFamily, FontSize = 13
  };
  dialog.SetResourceReference(Window.BackgroundProperty, "Canvas");
  dialog.SetResourceReference(Window.ForegroundProperty, "Ink");
  EnableEscapeClose(dialog);

  var root = new DockPanel { Margin = new Thickness(20) };
  dialog.Content = root;

  var header = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
  DockPanel.SetDock(header, Dock.Top);
  root.Children.Add(header);

  header.Children.Add(Label(I18n.T("PHÂN TÍCH TRỰC QUAN DUNG LƯỢNG THƯ MỤC (DISK SPACE ANALYZER)"), 19, true));
  header.Children.Add(Label(I18n.T("Đo lường và xếp hạng trực quan các thư mục chiếm nhiều dung lượng nhất trên ổ đĩa.\n★ Trả lời ngay câu hỏi: 'Thư mục nào đang ngốn đầy ổ cứng của tôi?'."), 12));

  var selRow = new WrapPanel { Margin = new Thickness(0, 6, 0, 4) };
  header.Children.Add(selRow);
  selRow.Children.Add(Label("Đường dẫn cần phân tích:  ", 12, true));
  var txtPath = new TextBox { Text = @"C:\", Width = 260, Margin = new Thickness(0, 0, 8, 0) };
  txtPath.SetResourceReference(Control.BackgroundProperty, "Header");
  selRow.Children.Add(txtPath);

  selRow.Children.Add(ActionButton("Ổ C:\\", () => txtPath.Text = @"C:\"));
  selRow.Children.Add(ActionButton("Thư mục cá nhân", () => txtPath.Text = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)));

  var bottom = new StackPanel { Margin = new Thickness(0, 12, 0, 0) };
  DockPanel.SetDock(bottom, Dock.Bottom);
  root.Children.Add(bottom);

  var statusText = Label(I18n.T("Bấm 'Bắt đầu phân tích' để tính toán dung lượng các thư mục."), 12);
  statusText.Margin = new Thickness(0, 0, 0, 6);
  bottom.Children.Add(statusText);

  var btnRow = new WrapPanel();
  bottom.Children.Add(btnRow);

  var grid = new DataGrid { IsReadOnly = true, AutoGenerateColumns = false, RowHeight = Double.NaN, MinRowHeight = 36 };
  root.Children.Add(grid);
  AddColumn(grid, "THƯ MỤC", "Name", 220);
  AddColumn(grid, "DUNG LƯỢNG", "SizeLabel", 130);
  AddColumn(grid, "TỶ LỆ", "PercentLabel", 90);
  AddColumn(grid, "SỐ TỆP TIN", "FileCount", 100);
  AddColumn(grid, "ĐƯỜNG DẪN ĐẦY ĐỦ", "FullPath", 360);

  List<FolderSpaceItem> items = new List<FolderSpaceItem>();
  var btnScan = ActionButton(I18n.T("🔍 Bắt đầu phân tích"), async () => {
   string path = txtPath.Text.Trim();
   if (!Directory.Exists(path)) { MessageBox.Show(dialog, "Đường dẫn không tồn tại.", "Thông báo"); return; }
   btnRow.IsEnabled = false;
   statusText.Text = "Đang quét và tính toán kích thước các thư mục con...";
   try {
    items = await Task.Run(() => FolderSpaceVisualizer.Analyze(path, CancellationToken.None, null));
    grid.ItemsSource = null;
    grid.ItemsSource = items;
    statusText.Text = string.Format("Phân tích xong {0} thư mục tại {1}. Tổng cộng: {2}.", items.Count, path, Features.Size(items.Sum(x => x.Bytes)));
    try { System.Media.SystemSounds.Asterisk.Play(); } catch {}
   } catch (Exception ex) {
    statusText.Text = "Lỗi khi phân tích: " + ex.Message;
   } finally {
    btnRow.IsEnabled = true;
   }
  });
  btnScan.Style = (Style)window.Resources["Primary"];
  selRow.Children.Add(btnScan);

  btnRow.Children.Add(ActionButton(I18n.T("📂 Mở thư mục trong Explorer"), () => {
   var sel = grid.SelectedItem as FolderSpaceItem;
   if (sel != null && Directory.Exists(sel.FullPath)) {
    Process.Start(new ProcessStartInfo("explorer.exe", sel.FullPath) { UseShellExecute = true });
   } else {
    MessageBox.Show(dialog, "Vui lòng chọn 1 thư mục trong bảng.", "Thông báo");
   }
  }));

  dialog.ShowDialog();
 }

 void OpenWindowsUpdateCleanerDialog() {
  var dialog = new Window {
   Title = I18n.T("Dọn Dẹp & Khắc Phục Lỗi Windows Update Cache · PurePC 1.2"),
   Owner = window, Width = 840, Height = 560, MinWidth = 700, MinHeight = 440,
   WindowStartupLocation = WindowStartupLocation.CenterOwner,
   Resources = window.Resources, FontFamily = window.FontFamily, FontSize = 13
  };
  dialog.SetResourceReference(Window.BackgroundProperty, "Canvas");
  dialog.SetResourceReference(Window.ForegroundProperty, "Ink");
  EnableEscapeClose(dialog);

  var root = new DockPanel { Margin = new Thickness(20) };
  dialog.Content = root;

  var header = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
  DockPanel.SetDock(header, Dock.Top);
  root.Children.Add(header);

  header.Children.Add(Label(I18n.T("DỌN DẸP & KHẮC PHỤC LỖI WINDOWS UPDATE CACHE"), 19, true));
  header.Children.Add(Label(I18n.T("Giải phóng hàng gigabyte tệp cập nhật tải về cũ hoặc bị lỗi trong thư mục C:\\Windows\\SoftwareDistribution\\Download, đồng thời tự động khởi động lại dịch vụ Update để sửa các mã lỗi 0x80070002, 0x80240034."), 12));

  var statusBox = new StackPanel();
  var lblSize = Label("...", 13, true);
  var lblCount = Label("...", 13, true);

  Action checkCache = () => {
   int count;
   long bytes = WindowsUpdateCleaner.GetCacheBytes(out count);
   lblSize.Text = Features.Size(bytes);
   lblSize.Foreground = bytes > 1024L * 1024 * 1024 ? Brushes.OrangeRed : Brushes.MediumSeaGreen;
   lblCount.Text = count + " tệp gói bản vá";
  };
  checkCache();

  var sGrid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
  sGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(240) });
  sGrid.ColumnDefinitions.Add(new ColumnDefinition());
  sGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
  sGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

  var r1 = Label("Dung lượng bộ đệm Windows Update:", 12); r1.SetResourceReference(TextBlock.ForegroundProperty, "Muted");
  var r2 = Label("Số tệp bản vá lưu trữ:", 12); r2.SetResourceReference(TextBlock.ForegroundProperty, "Muted");

  Grid.SetRow(r1, 0); Grid.SetColumn(r1, 0); sGrid.Children.Add(r1);
  Grid.SetRow(lblSize, 0); Grid.SetColumn(lblSize, 1); sGrid.Children.Add(lblSize);
  Grid.SetRow(r2, 1); Grid.SetColumn(r2, 0); sGrid.Children.Add(r2);
  Grid.SetRow(lblCount, 1); Grid.SetColumn(lblCount, 1); sGrid.Children.Add(lblCount);

  statusBox.Children.Add(sGrid);
  var cardStatus = Card(statusBox);
  cardStatus.Padding = new Thickness(14, 10, 14, 10);
  header.Children.Add(cardStatus);

  var bottom = new StackPanel { Margin = new Thickness(0, 12, 0, 0) };
  DockPanel.SetDock(bottom, Dock.Bottom);
  root.Children.Add(bottom);

  var statusText = Label(I18n.T("Chọn 'Dọn sạch Cache & Làm mới Windows Update' để giải phóng bộ đệm."), 12);
  statusText.Margin = new Thickness(0, 0, 0, 6);
  bottom.Children.Add(statusText);

  var btnRow = new WrapPanel();
  bottom.Children.Add(btnRow);

  var logBox = new TextBox {
   IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap,
   VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
   FontFamily = new FontFamily("Consolas"), FontSize = 11,
   Margin = new Thickness(0, 4, 0, 0),
   Text = "Nhật ký làm mới Windows Update sẵn sàng..."
  };
  logBox.SetResourceReference(Control.BackgroundProperty, "Header");
  logBox.SetResourceReference(Control.ForegroundProperty, "Ink");
  root.Children.Add(logBox);

  var btnPurge = ActionButton(I18n.T("🧹 Dọn sạch Cache & Làm mới Windows Update"), async () => {
   if (!Confirm("Tạm dừng dịch vụ Windows Update và dọn sạch các gói tải về trong SoftwareDistribution\\Download?\nQuá trình sẽ tự động khởi động lại dịch vụ ngay sau khi dọn xong.")) return;
   btnRow.IsEnabled = false;
   statusText.Text = "Đang dọn dẹp bộ đệm Windows Update...";
   try {
    string report = "";
    long freed = 0;
    await Task.Run(() => WindowsUpdateCleaner.Purge(out report, out freed));
    logBox.Text = report;
    checkCache();
    try { System.Media.SystemSounds.Asterisk.Play(); } catch {}
    statusText.Text = string.Format("Đã dọn sạch {0} tệp bản cập nhật cũ.", Features.Size(freed));
    MessageBox.Show(dialog, string.Format("Đã làm sạch thành công {0} cache Windows Update!", Features.Size(freed)), "Hoàn tất", MessageBoxButton.OK, MessageBoxImage.Information);
   } catch (Exception ex) {
    statusText.Text = "Lỗi khi dọn dẹp: " + ex.Message;
    logBox.AppendText("\r\n[Lỗi] " + ex.Message + "\r\n");
   } finally {
    btnRow.IsEnabled = true;
   }
  });
  btnPurge.Style = (Style)window.Resources["Primary"];
  btnRow.Children.Add(btnPurge);

  btnRow.Children.Add(ActionButton(I18n.T("Kiểm tra lại dung lượng"), () => { checkCache(); statusText.Text = "Đã cập nhật dung lượng cache."; }));

  dialog.ShowDialog();
 }
}
