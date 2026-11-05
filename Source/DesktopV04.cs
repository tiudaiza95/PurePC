using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

partial class DesktopApp {
 List<DeepItem> deepItems=new List<DeepItem>();
 List<DriverRecord> driverRecords=new List<DriverRecord>();

 List<BrokenShortcut> brokenShortcuts=new List<BrokenShortcut>();
 List<DevAiCacheItem> devAiItems=new List<DevAiCacheItem>();
 DataGrid driversGrid;
 TextBlock driverStatus;

 void BuildV04(){
  var navStack=(StackPanel)C<RadioButton>("NavApps").Parent;
  AddNavV04(navStack,"Deep","Quét chuyên sâu",BuildDeepPage,navStack.Children.IndexOf(C<RadioButton>("NavApps"))+1);
  AddNavV04(navStack,"DiskTools","Công cụ ổ đĩa",BuildDiskTools,navStack.Children.IndexOf(C<RadioButton>("NavStartup"))+1);
  AddNavV04(navStack,"Drivers","Driver & thiết bị",BuildDriverPage,navStack.Children.IndexOf(C<RadioButton>("NavHistory")));

  var menu=C<DataGrid>("FilesGrid").ContextMenu;
  AddMenu(menu,"Chuyển các tệp được đánh dấu vào Thùng rác",()=>RunDetached(CleanFilesPro));
 }

 void AddNavV04(StackPanel parent,string name,string caption,Action build,int index){
  var nav=new RadioButton{Content=caption,GroupName="Nav",Style=(Style)window.Resources["Nav"],ToolTip=caption};
  window.RegisterName("Nav"+name,nav);
  nav.Checked+=(s,e)=>{if(!busy)OpenV04(name,build);};
  if(index>=0&&index<=parent.Children.Count)parent.Children.Insert(index,nav);
  else parent.Children.Add(nav);
  proControls.Add(nav);
 }

 void OpenV04(string name,Action build){
  page=name;
  foreach(string panel in new[]{"AppsPage","FilesPage","StartupPage","HistoryPage"})C<FrameworkElement>(panel).Visibility=Visibility.Collapsed;
  HidePro();
  proHost.Visibility=Visibility.Visible;
  proHost.Children.Clear();
  build();
 }

 static void OpenNative(string file){
  string path=Path.Combine(Environment.SystemDirectory,file);
  if(!File.Exists(path))throw new Exception("Windows không có công cụ "+file+" trên máy này.");
  Process.Start(new ProcessStartInfo(path){UseShellExecute=true});
 }

 void RequestAdministrator(){
  if(Drivers.Administrator()){Text("Status","Ứng dụng đã có quyền Administrator.");return;}
  if(!Confirm("Mở một phiên PurePC với quyền Administrator?\nWindows sẽ hiện UAC để bạn tự xác nhận. Chưa tự gỡ, dọn hay cài driver. Phiên hiện tại được giữ nguyên."))return;
  Process.Start(new ProcessStartInfo(Process.GetCurrentProcess().MainModule.FileName,"--drivers"){UseShellExecute=true,Verb="runas"});
 }

 async Task DeleteLargeFiles(){
  var chosen=files.Where(x=>x.Selected).ToList();
  if(chosen.Count==0){Text("Status","Đánh dấu tệp muốn xóa trước.");return;}
  if(!Confirm("Chuyển "+chosen.Count+" tệp ("+Features.Size(chosen.Sum(x=>x.Size))+") vào Thùng rác?\n\n"+String.Join("\n",chosen.Take(8).Select(x=>x.Path))+(chosen.Count>8?"\n… xem toàn bộ trong bảng đã chọn":"")+"\n\nKhông thể tự phục hồi nếu bạn đồng ý xóa vĩnh viễn trong hộp thoại Windows. Tệp hệ thống/chương trình được giữ lại."))return;
  int done=0;var errors=new List<string>();
  foreach(var item in chosen){
   try{FileActions.Recycle(item);files.Remove(item);done++;}catch(Exception ex){errors.Add(ex.Message);}
   await Task.Delay(1);
  }
  C<DataGrid>("FilesGrid").ItemsSource=null;C<DataGrid>("FilesGrid").ItemsSource=files;
  Text("FilesSummary","Đã xử lý "+done+" / "+chosen.Count+" tệp; còn "+files.Count+" dòng trong kết quả.");
  Text("Status","Tệp đã chuyển vào Thùng rác an toàn.");
  ShowErrors(errors);
 }

 void ShowErrors(List<string> errors){
  if(errors.Count>0){
   foreach(var error in errors)Engine.Log("ACTION_SKIPPED "+error);
   MessageBox.Show(window,String.Join("\n\n",errors.Take(8)),"Các mục được giữ lại");
  }
 }

 void AddChecks(DataGrid grid,string property="Selected",string enabled=null){
  var template=new DataTemplate();
  var box=new FrameworkElementFactory(typeof(CheckBox));
  box.SetBinding(CheckBox.IsCheckedProperty,new Binding(property){Mode=BindingMode.TwoWay,UpdateSourceTrigger=UpdateSourceTrigger.PropertyChanged});
  box.SetValue(CheckBox.HorizontalAlignmentProperty,HorizontalAlignment.Center);
  if(enabled!=null)box.SetBinding(CheckBox.IsEnabledProperty,new Binding(enabled));
  template.VisualTree=box;
  grid.Columns.Insert(0,new DataGridTemplateColumn{Header="CHỌN",Width=52,CellTemplate=template});
 }

 void AddDuplicateActions(StackPanel panel,DataGrid grid){
  AddChecks(grid);
  var actions=new WrapPanel();panel.Children.Add(actions);
  var automatic=ActionButton("Tự chọn bản trùng để dọn",()=>{
   if(duplicateFiles==null||duplicateFiles.Count==0){Text("Status","Quét thư mục để tìm tệp trùng trước.");return;}
   int count=WholeMachine.SelectDuplicateExtras(duplicateFiles);
   grid.Items.Refresh();
   if(progressText!=null)progressText.Text="Đã chọn "+count+" bản dư · "+Features.Size(duplicateFiles.Where(x=>x.Selected).Sum(x=>x.Size))+". Giữ một bản trong mỗi nhóm.";
  });
  automatic.Style=(Style)window.Resources["Primary"];
  actions.Children.Add(automatic);
  actions.Children.Add(ActionButton("Giữ tệp này, chọn các bản còn lại",()=>{
   var keep=grid.SelectedItem as FileItem;
   if(keep==null){Text("Status","Chọn dòng muốn giữ trước.");return;}
   foreach(var file in duplicateFiles.Where(x=>x.Group==keep.Group))file.Selected=file.Path!=keep.Path;
   grid.Items.Refresh();
   Text("Status","Đã chọn các bản khác trong "+keep.Group+"; chưa xóa tệp nào.");
  }));
  actions.Children.Add(WorkButton("Dọn bản đã chọn",async()=>{
   var chosen=duplicateFiles==null?new List<FileItem>():duplicateFiles.Where(x=>x.Selected).ToList();
   if(chosen.Count==0){Text("Status","Chưa đánh dấu bản trùng muốn xóa.");return;}
   if(!Confirm("Chuyển "+chosen.Count+" bản trùng đã đánh dấu vào Thùng rác?\nỨng dụng sẽ đọc lại nội dung và yêu cầu ít nhất một bản giữ lại trong từng nhóm.\n"+String.Join("\n",chosen.Take(6).Select(x=>x.Path))))return;
   var deleting=new HashSet<string>(chosen.Select(x=>x.Path),StringComparer.OrdinalIgnoreCase);
   int done=0;var errors=new List<string>();
   foreach(var item in chosen){
    try{
     Text("Status","Đối chiếu lại nội dung: "+item.Name);
     await Task.Run(()=>FileActions.ValidateDuplicate(item,duplicateFiles,deleting));
     FileActions.Recycle(item);
     duplicateFiles.Remove(item);
     done++;
    }catch(Exception ex){errors.Add(ex.Message);}
   }
   grid.ItemsSource=null;grid.ItemsSource=duplicateFiles;
   Text("Status","Đã xử lý "+done+" bản trùng; giữ lại "+errors.Count+" mục.");
   ShowErrors(errors);
  }));
 }

 void BuildDeepPage(){
  Text("Heading",I18n.T("Quét chuyên sâu toàn diện hệ thống"));
  Text("Subtitle",I18n.T("Tích hợp 5 trong 1: Tàn dư gỡ cài đặt · Tệp dung lượng lớn (>100MB) · Tệp trùng lặp · Lối tắt hỏng · Rác Lập trình & AI"));
  var dock=new DockPanel();
  proHost.Children.Add(dock);

  var top=new StackPanel();
  DockPanel.SetDock(top,Dock.Top);
  dock.Children.Add(top);

  // Thanh chuyển chế độ 3 trong 1
  var modeRow=new WrapPanel{Margin=new Thickness(0,0,0,10)};
  top.Children.Add(modeRow);

  var btnTabDeep=ActionButton(I18n.T("🛡️ Tàn dư gỡ cài đặt")+" ("+deepItems.Count+")",()=>{});
  var btnTabLarge=ActionButton(I18n.T("📦 Tệp dung lượng lớn")+" ("+files.Count+")",()=>{});
  var btnTabDup=ActionButton(I18n.T("📑 Tệp trùng lặp")+" ("+(duplicateFiles==null?0:duplicateFiles.Count)+")",()=>{});
  var btnTabShort=ActionButton(I18n.T("🔗 Lối tắt hỏng")+" ("+brokenShortcuts.Count+")",()=>{});
  var btnTabDev=ActionButton(I18n.T("💻 Rác Lập trình & AI")+" ("+devAiItems.Count+")",()=>{});

  modeRow.Children.Add(btnTabDeep);
  modeRow.Children.Add(btnTabLarge);
  modeRow.Children.Add(btnTabDup);
  modeRow.Children.Add(btnTabShort);
  modeRow.Children.Add(btnTabDev);

  // 3 Grid tương ứng
  var deepGrid=new DataGrid{IsReadOnly=true,ItemsSource=deepItems};
  AddChecks(deepGrid,"Selected","Actionable");
  AddColumn(deepGrid,"ỨNG DỤNG","Name",190);
  AddColumn(deepGrid,"BẰNG CHỨNG","Evidence",300);
  AddColumn(deepGrid,"HƯỚNG XỬ LÝ","Action",220);
  AddColumn(deepGrid,"VỊ TRÍ","Path",360);

  var largeGrid=new DataGrid{IsReadOnly=true,ItemsSource=files};
  AddChecks(largeGrid,"Selected");
  AddColumn(largeGrid,"TÊN TỆP","Name",240);
  AddColumn(largeGrid,"DUNG LƯỢNG","SizeLabel",110);
  AddColumn(largeGrid,"ĐƯỜNG DẪN","Path",480);

  var dupGrid=new DataGrid{IsReadOnly=true,ItemsSource=duplicateFiles};
  AddColumn(dupGrid,"NHÓM","Group",80);
  AddColumn(dupGrid,"DUNG LƯỢNG","SizeLabel",110);
  AddColumn(dupGrid,"ĐƯỜNG DẪN","Path",520);
  AddColumn(dupGrid,"SHA-256","Evidence",440);

  var shortGrid=new DataGrid{IsReadOnly=true,ItemsSource=brokenShortcuts};
  AddChecks(shortGrid,"Selected");
  AddColumn(shortGrid,"TÊN LỐI TẮT","Name",200);
  AddColumn(shortGrid,"VỊ TRÍ","LocationCategory",180);
  AddColumn(shortGrid,"ĐƯỜNG DẪN HỎNG","TargetPath",380);
  AddColumn(shortGrid,"LÝ DO","Reason",260);

  var contentHolder=new Grid();
  dock.Children.Add(contentHolder);
  var devAiGrid=new DataGrid{IsReadOnly=true,ItemsSource=devAiItems};
  AddChecks(devAiGrid,"Selected");
  AddColumn(devAiGrid,"PHÂN LOẠI","Category",180);
  AddColumn(devAiGrid,"CÔNG CỤ / NỀN TẢNG","ToolName",170);
  AddColumn(devAiGrid,"SỐ TỆP","FileCount",80);
  AddColumn(devAiGrid,"DUNG LƯỢNG RÁC","SizeLabel",130);
  AddColumn(devAiGrid,"ĐƯỜNG DẪN THƯ MỤC","Path",360);

  contentHolder.Children.Add(deepGrid);
  contentHolder.Children.Add(largeGrid);
  contentHolder.Children.Add(dupGrid);
  contentHolder.Children.Add(shortGrid);
  contentHolder.Children.Add(devAiGrid);

  var emptyCard=Card(new StackPanel());
  emptyCard.HorizontalAlignment=HorizontalAlignment.Center;
  emptyCard.VerticalAlignment=VerticalAlignment.Center;
  emptyCard.Padding=new Thickness(32,24,32,24);
  var emptyStack=(StackPanel)emptyCard.Child;
  var emptyIcon=Label("🔍",24,true);emptyIcon.HorizontalAlignment=HorizontalAlignment.Center;emptyIcon.Margin=new Thickness(0,0,0,8);
  var emptyText=Label("Chưa có dữ liệu cho mục này. Hãy bấm '⚡ Quét toàn bộ chuyên sâu (Cả 3 mục)' để bắt đầu phân tích!",13,true);
  emptyText.HorizontalAlignment=HorizontalAlignment.Center;
  emptyStack.Children.Add(emptyIcon);
  emptyStack.Children.Add(emptyText);
  contentHolder.Children.Add(emptyCard);

  int currentTab=0;
  bool hasScanned=false;
  Button btnCleanDeep=null, btnCleanLarge=null, btnCleanDev=null, btnCleanShort=null, btnAutoDup=null, btnCleanDup=null;
  Action updateTabUI=()=>{
   btnTabDeep.Content=I18n.T("🛡️ Tàn dư gỡ cài đặt")+" ("+deepItems.Count+")";
   btnTabLarge.Content=I18n.T("📦 Tệp dung lượng lớn")+" ("+files.Count+")";
   btnTabDup.Content=I18n.T("📑 Tệp trùng lặp")+" ("+(duplicateFiles==null?0:duplicateFiles.Count)+")";
   btnTabShort.Content=I18n.T("🔗 Lối tắt hỏng")+" ("+brokenShortcuts.Count+")";
   btnTabDev.Content=I18n.T("💻 Rác Lập trình & AI")+" ("+devAiItems.Count+")";
   btnTabDeep.Style=currentTab==0?(Style)window.Resources["Primary"]:(Style)window.FindResource(typeof(Button));
   btnTabLarge.Style=currentTab==1?(Style)window.Resources["Primary"]:(Style)window.FindResource(typeof(Button));
   btnTabDup.Style=currentTab==2?(Style)window.Resources["Primary"]:(Style)window.FindResource(typeof(Button));
   btnTabShort.Style=currentTab==3?(Style)window.Resources["Primary"]:(Style)window.FindResource(typeof(Button));
   btnTabDev.Style=currentTab==4?(Style)window.Resources["Primary"]:(Style)window.FindResource(typeof(Button));
   int curCount=currentTab==0?deepItems.Count:(currentTab==1?files.Count:(currentTab==2?(duplicateFiles==null?0:duplicateFiles.Count):(currentTab==3?brokenShortcuts.Count:devAiItems.Count)));
   emptyCard.Visibility=curCount==0?Visibility.Visible:Visibility.Collapsed;
   if(curCount==0){
    emptyText.Text=hasScanned
     ? I18n.T("Không phát hiện mục nào cần xử lý trong phần này. Hệ thống rất sạch sẽ!")
     : I18n.T("Chưa có dữ liệu cho mục này. Hãy bấm '⚡ Quét toàn bộ chuyên sâu' để bắt đầu phân tích!");
    emptyIcon.Text=hasScanned?"✅":"🔍";
   }
   deepGrid.Visibility=(currentTab==0&&curCount>0)?Visibility.Visible:Visibility.Collapsed;
   largeGrid.Visibility=(currentTab==1&&curCount>0)?Visibility.Visible:Visibility.Collapsed;
   dupGrid.Visibility=(currentTab==2&&curCount>0)?Visibility.Visible:Visibility.Collapsed;
   shortGrid.Visibility=(currentTab==3&&curCount>0)?Visibility.Visible:Visibility.Collapsed;
   devAiGrid.Visibility=(currentTab==4&&curCount>0)?Visibility.Visible:Visibility.Collapsed;
   if(btnCleanDeep!=null)btnCleanDeep.Visibility=(currentTab==0&&deepItems.Count>0)?Visibility.Visible:Visibility.Collapsed;
   if(btnCleanLarge!=null)btnCleanLarge.Visibility=(currentTab==1&&files.Count>0)?Visibility.Visible:Visibility.Collapsed;
   if(btnAutoDup!=null)btnAutoDup.Visibility=(currentTab==2&&(duplicateFiles!=null&&duplicateFiles.Count>0))?Visibility.Visible:Visibility.Collapsed;
   if(btnCleanDup!=null)btnCleanDup.Visibility=(currentTab==2&&(duplicateFiles!=null&&duplicateFiles.Count>0))?Visibility.Visible:Visibility.Collapsed;
   if(btnCleanShort!=null)btnCleanShort.Visibility=(currentTab==3&&brokenShortcuts.Count>0)?Visibility.Visible:Visibility.Collapsed;
   if(btnCleanDev!=null)btnCleanDev.Visibility=(currentTab==4&&devAiItems.Count>0)?Visibility.Visible:Visibility.Collapsed;
  };

  btnTabDeep.Click+=(s,e)=>{currentTab=0;updateTabUI();};
  btnTabLarge.Click+=(s,e)=>{currentTab=1;updateTabUI();};
  btnTabDup.Click+=(s,e)=>{currentTab=2;updateTabUI();};
  btnTabShort.Click+=(s,e)=>{currentTab=3;updateTabUI();};
  btnTabDev.Click+=(s,e)=>{currentTab=4;updateTabUI();};

  // Nút hành động chính và các nút theo ngữ cảnh tab
  var actionRow=new WrapPanel{Margin=new Thickness(0,0,0,10)};
  top.Children.Add(actionRow);

  var btnScanAll=WorkButton(I18n.T("⚡ Quét toàn bộ chuyên sâu"),async()=>{
   await WithScan(async token=>{
    var progress=Progress();
    hasScanned=true;

    Text("Status","[1/5] Đang quét tàn dư gỡ cài đặt và khóa Registry mồ côi…");
    deepItems=await Task.Run(()=>DeepClean.Scan(token,progress));
    deepGrid.ItemsSource=null;deepGrid.ItemsSource=deepItems;
    updateTabUI();

    Text("Status","[2/5] Đang phân tích tệp dung lượng lớn (>100MB) trên các ổ đĩa…");
    string userDir=Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    int skipped=0;bool limited=false;
    var allFiles=await Task.Run(()=>AnalysisTools.Walk(userDir,token,progress,out skipped,out limited,AnalysisTools.IsJunkOrSystemCacheDir));
    foreach(var d in DriveInfo.GetDrives()){
     if(d.IsReady&&d.DriveType==DriveType.Fixed&&!d.RootDirectory.FullName.StartsWith("C",StringComparison.OrdinalIgnoreCase)){
      try{
       int dSkip=0;bool dLim=false;
       var dFiles=await Task.Run(()=>AnalysisTools.Walk(d.RootDirectory.FullName,token,progress,out dSkip,out dLim,AnalysisTools.IsJunkOrSystemCacheDir));
       allFiles.AddRange(dFiles);skipped+=dSkip;
      }catch{}
     }
    }
    files=allFiles.Where(x=>x.Size>=100L*1024*1024).OrderByDescending(x=>x.Size).Take(300).ToList();
    largeGrid.ItemsSource=null;largeGrid.ItemsSource=files;
    updateTabUI();

    Text("Status","[3/5] Đang kiểm tra đối chiếu nội dung trùng lặp (SHA-256)…");
    string summary="";
    duplicateFiles=await Task.Run(()=>AnalysisTools.Duplicates(allFiles,token,progress,out summary));
    dupGrid.ItemsSource=null;dupGrid.ItemsSource=duplicateFiles;
    updateTabUI();

    Text("Status","[4/5] Đang quét các lối tắt (.lnk) bị hỏng trên Desktop & Start Menu…");
    brokenShortcuts=await Task.Run(()=>ShortcutCleaner.Scan());
    shortGrid.ItemsSource=null;shortGrid.ItemsSource=brokenShortcuts;
    updateTabUI();

    Text("Status","[5/5] Đang kiểm tra các kho đệm lập trình & mô hình AI…");
    devAiItems=await Task.Run(()=>DevAiCacheCleaner.ScanAll(token,null));
    devAiGrid.ItemsSource=null;devAiGrid.ItemsSource=devAiItems;
    updateTabUI();

    Text("Status","Quét toàn diện hoàn tất: "+deepItems.Count+" tàn dư · "+files.Count+" tệp lớn · "+(duplicateFiles==null?0:duplicateFiles.Count)+" tệp trùng · "+brokenShortcuts.Count+" lối tắt · "+devAiItems.Count+" rác Dev/AI.");
   });
  });
  btnScanAll.Style=(Style)window.Resources["Primary"];
  btnScanAll.Margin=new Thickness(0,0,10,0);
  actionRow.Children.Add(btnScanAll);

  // Nút xử lý tàn dư (chỉ hiện khi ở Tab 0)
  btnCleanDeep=WorkButton(I18n.T("🧹 Dọn tàn dư đã chọn"),async()=>{ await Task.Yield();
   var selected=deepItems.Where(x=>x.Selected&&x.Actionable).ToList();
   if(selected.Count==0){Text("Status","Chưa chọn mục tàn dư có đủ điều kiện xử lý.");return;}
   if(!Confirm("Xử lý "+selected.Count+" mục tàn dư theo hành động?\nCác mục sẽ được chuyển vào Thùng rác để an toàn."))return;
   int done=0;var errors=new List<string>();
   foreach(var item in selected){
    try{
     if(item.Remnant!=null)Engine.Clean(item.App,item.Remnant);
     else if(item.Stale)DeepClean.RemoveStale(item.App);
     done++;deepItems.Remove(item);
    }catch(Exception ex){errors.Add(ex.Message);}
   }
   deepGrid.ItemsSource=null;deepGrid.ItemsSource=deepItems;
   updateTabUI();
   Text("Status","Đã xử lý "+done+" mục tàn dư; giữ lại "+errors.Count+" mục.");
   ShowErrors(errors);
  });
  btnCleanDeep.Margin=new Thickness(0,0,8,0);
  actionRow.Children.Add(btnCleanDeep);

  // Nút dọn tệp lớn (chỉ hiện khi ở Tab 1)
  btnCleanLarge=WorkButton(I18n.T("🧹 Dọn tệp lớn đã chọn"),DeleteLargeFiles);
  btnCleanLarge.Margin=new Thickness(0,0,8,0);
  actionRow.Children.Add(btnCleanLarge);

  // Nút chọn tự động tệp trùng (chỉ hiện khi ở Tab 2)
  btnAutoDup=ActionButton(I18n.T("✨ Tự chọn bản trùng dư thừa"),()=>{
   if(duplicateFiles==null||duplicateFiles.Count==0){Text("Status","Chưa có dữ liệu tệp trùng.");return;}
   int count=WholeMachine.SelectDuplicateExtras(duplicateFiles);
   dupGrid.Items.Refresh();
   currentTab=2;updateTabUI();
   Text("Status","Đã chọn "+count+" bản sao dư thừa để dọn.");
  });
  btnAutoDup.Margin=new Thickness(0,0,8,0);
  actionRow.Children.Add(btnAutoDup);

  // Nút dọn tệp trùng đã chọn (chỉ hiện khi ở Tab 2)
  btnCleanDup=WorkButton(I18n.T("🧹 Dọn tệp trùng đã chọn"),async()=>{ await Task.Yield();
   var chosen=duplicateFiles==null?new List<FileItem>():duplicateFiles.Where(x=>x.Selected).ToList();
   if(chosen.Count==0){Text("Status","Chưa đánh dấu bản trùng muốn xóa. Hãy bấm 'Tự chọn bản trùng dư thừa' hoặc chọn thủ công.");return;}
   if(!Confirm("Chuyển "+chosen.Count+" bản trùng đã đánh dấu vào Thùng rác?\nỨng dụng sẽ giữ lại ít nhất một bản gốc trong từng nhóm."))return;
   var deleting=new HashSet<string>(chosen.Select(x=>x.Path),StringComparer.OrdinalIgnoreCase);
   int done=0;var errors=new List<string>();
   foreach(var item in chosen){
    try{
     Text("Status","Đối chiếu & dọn dẹp: "+item.Name);
     await Task.Run(()=>FileActions.ValidateDuplicate(item,duplicateFiles,deleting));
     FileActions.Recycle(item);
     duplicateFiles.Remove(item);
     done++;
    }catch(Exception ex){errors.Add(ex.Message);}
   }
   dupGrid.ItemsSource=null;dupGrid.ItemsSource=duplicateFiles;
   updateTabUI();
   Text("Status","Đã xử lý "+done+" bản trùng; giữ lại "+errors.Count+" mục.");
   ShowErrors(errors);
  });
  btnCleanDup.Margin=new Thickness(0,0,8,0);
  actionRow.Children.Add(btnCleanDup);

  // Nút dọn lối tắt hỏng (chỉ hiện khi ở Tab 3)
  btnCleanShort=WorkButton(I18n.T("🧹 Dọn lối tắt đã chọn"),async()=>{ await Task.Yield();
   var chosen=brokenShortcuts.Where(x=>x.Selected).ToList();
   if(chosen.Count==0){Text("Status","Chưa chọn lối tắt hỏng.");return;}
   if(!Confirm("Chuyển "+chosen.Count+" lối tắt hỏng vào Thùng rác để an toàn?"))return;
   List<string> errors;
   int cleaned=ShortcutCleaner.Clean(chosen,out errors);
   foreach(var item in chosen)brokenShortcuts.Remove(item);
   shortGrid.ItemsSource=null;shortGrid.ItemsSource=brokenShortcuts;
   updateTabUI();
   Text("Status","Đã dọn "+cleaned+" lối tắt hỏng; giữ lại "+errors.Count+" mục.");
   ShowErrors(errors);
  });
  btnCleanShort.Margin=new Thickness(0,0,8,0);
  actionRow.Children.Add(btnCleanShort);

  // Nút dọn rác Dev & AI (chỉ hiện khi ở Tab 4)
  btnCleanDev=WorkButton(I18n.T("🧹 Dọn rác Dev & AI đã chọn"),async()=>{ await Task.Yield();
   var chosen=devAiItems.Where(x=>x.Selected).ToList();
   if(chosen.Count==0){Text("Status","Chưa chọn kho bộ đệm nào.");return;}
   long total=chosen.Sum(x=>x.Bytes);
   if(!Confirm("Dọn sạch "+chosen.Count+" thư mục bộ đệm lập trình & AI ("+Features.Size(total)+")?\nToàn bộ tệp sẽ được chuyển vào Thùng rác để đảm bảo an toàn."))return;
   int done=0;
   foreach(var item in chosen){
    if(DevAiCacheCleaner.Clean(item,true)){done++;devAiItems.Remove(item);}
   }
   devAiGrid.ItemsSource=null;devAiGrid.ItemsSource=devAiItems;
   updateTabUI();
   Text("Status","Đã dọn sạch "+done+" mục bộ đệm lập trình & AI.");
  });
  btnCleanDev.Margin=new Thickness(0,0,8,0);
  actionRow.Children.Add(btnCleanDev);

  updateTabUI();

  top.Children.Add(Label("Bấm tab tương ứng để xem bảng kết quả chi tiết của từng loại tệp. Mọi thao tác xóa đều ưu tiên Thùng rác để có thể phục hồi.",11));
 }
}