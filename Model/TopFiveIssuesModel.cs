using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LuisBot.Model
{
    public class TopFiveIssuesModel
    {
        public string Key { get; set; }
        public int Values { get; set; }
        public bool Totickets { get; set; }

    }
    public class TopFiveissue
    {
        public List<TopFiveIssuesModel> test { get; set; }
    }
}