using Inventory2.Data;
using Inventory2.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using XQLite;

namespace Inventory2
{
    public class DbProvider : IDatabaseProvider
    {
        public string ConnectionString(string scheme)
        {
            var dir = System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var db = Path.Combine(dir, "Inventory.sqlite3");
            return db;
        }
        public string[] DataSchemes()
        {
            return new string[1] { "Inventory.sqlite3" };
        }
    }
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
      
        }

        private void FillMenu()
        {
            ListMenu.ItemsSource = new List<object>() {
                new AppMenuItem(
                    id: 0,
                    icon : "importar.png",
                    title : "Import Products",
                    subtitle: "Import Products From a Worksheet"
                    ),          
                new AppMenuItem(
                    icon : "expeditions_blue.png",
                    id : 1,
                    title : "Expeditions",
                    subtitle : "Separate products"
                    ),
                new AppMenuItem(
                    icon : "inventory_blue.png",
                    id : 2,
                    title : "Inventory",
                    subtitle : "Update the actual quantity of a product"
                    ),
                new AppMenuItem(
                    id : 3,
                    icon : "products_blue.png",
                    title : "Manage Products",
                    subtitle : "View Products and Check Addresses"
                    )
            };
        }

        private void ListMenu_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            AppMenuItem item = (AppMenuItem)ListMenu.SelectedItem;
            if (item.Id == 0) Navigation.PushAsync(new ImportProducts());
            if (item.Id == 3) Navigation.PushAsync(new ManageProducts());
        }
        private void ContentPage_Appearing(object sender, EventArgs e)
        {
            try
            {
                FillMenu();
                using (SQLiteContext db = new SQLiteContext())
                {
                    db.Initialize();
                }
                XQLiteServer.Init(
                    dbProviderType: typeof(DbProvider),
                    appName: "Inventory",
                    appVersion: "1.0.0",
                    accessLogin: "app",
                    accessPassword: "1234",
                    securityKey: "@utomac1020$#"
                 );
            }
            catch
            {

            }

        }
    }
}
