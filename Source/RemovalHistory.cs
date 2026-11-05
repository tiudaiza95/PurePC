using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Serialization;
static partial class Engine {
 public static void RememberHistory(AppEntry a){string dir=Path.Combine(Data,"Uninstall-history");Directory.CreateDirectory(dir);string path=Path.Combine(dir,DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")+"-"+Guid.NewGuid().ToString("N")+".xml");using(var file=File.Create(path))new XmlSerializer(typeof(AppEntry)).Serialize(file,a);}
 public static List<AppEntry> RemovalHistory(){var list=new List<AppEntry>();string dir=Path.Combine(Data,"Uninstall-history");if(Directory.Exists(dir))foreach(string path in Directory.GetFiles(dir,"*.xml").OrderByDescending(x=>x).Take(200))try{using(var stream=File.OpenRead(path))list.Add((AppEntry)new XmlSerializer(typeof(AppEntry)).Deserialize(stream));}catch{}var last=Recall();if(last!=null)list.Add(last);return list.GroupBy(x=>x.Id).Select(g=>g.First()).ToList();}
}
partial class DesktopApp {
 void ChooseRemovalHistory(){var records=Engine.RemovalHistory();if(records.Count==0){Text("Status","Chưa có lần gỡ được ghi nhận. Không đoán ứng dụng đã gỡ trước khi cài công cụ.");return;}var dialog=new Window{Title="Phần dư từ lịch sử gỡ",Owner=window,Width=760,Height=520,Resources=window.Resources,WindowStartupLocation=WindowStartupLocation.CenterOwner};dialog.SetResourceReference(Window.BackgroundProperty,"Canvas");EnableEscapeClose(dialog);var panel=new DockPanel{Margin=new Thickness(22)};dialog.Content=panel;var note=Label("Chỉ liệt kê các lần gỡ đã được ứng dụng ghi nhận. Nếu khóa cài đặt vẫn còn, quét sẽ bị chặn. Không suy đoán toàn bộ lịch sử gỡ của Windows.");DockPanel.SetDock(note,Dock.Top);panel.Children.Add(note);var grid=new DataGrid{IsReadOnly=true,ItemsSource=records};AddColumn(grid,"ỨNG DỤNG","Title",300);AddColumn(grid,"PHIÊN BẢN","Release",130);AddColumn(grid,"PHẠM VI","Scope",140);var scan=ActionButton("Quét phần dư mục này",()=>{var a=grid.SelectedItem as AppEntry;if(a==null)return;target=a;dialog.Close();Text("SelectedApp",a.Name);RunDetached(async()=>{remnants=await Task.Run(()=>Engine.Scan(a));BindRemnants();Text("Status",remnants.Count+" mục đề xuất · chỉ xem Registry chưa đủ bằng chứng · chưa dọn dữ liệu.");});});DockPanel.SetDock(scan,Dock.Bottom);panel.Children.Add(scan);panel.Children.Add(grid);dialog.ShowDialog();}
}
