using Inventory2.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Inventory2
{
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
                    id : 0,
                    icon : "products_blue.png",
                    title : "Manage Products",
                    subtitle : "Import and View Products"),
                new AppMenuItem(icon : "expeditions_blue.png",
                    id : 1,
                    title : "Expeditions",
                    subtitle : "Separate products"),
                new AppMenuItem(icon : "inventory_blue.png",
                    id : 2,
                    title : "Inventory",
                    subtitle : "Update the actual quantities of the product")
            };
        }

        private void ListMenu_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            AppMenuItem item = (AppMenuItem)ListMenu.SelectedItem;
            if (item.Id == 0)
                Navigation.PushAsync( new ManageProducts());
        }

        private void ContentPage_Appearing(object sender, EventArgs e)
        {
            FillMenu();
        }
    }
}
