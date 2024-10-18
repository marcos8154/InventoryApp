using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory2.Models
{
    internal class AppMenuItem
    {
        public AppMenuItem(string icon, string title, string subtitle, int id)
        {
            Icon = icon;
            Title = title;
            Subtitle = subtitle;
            Id = id;
        }

        public String Icon { get; set; }
        public String Title { get; set; }
        public String Subtitle { get; set; }
        public int Id { get; internal set; }
    }
}
