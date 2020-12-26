using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace covid19_form
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var iso = txtIso.Text;
            var client = new RestClient($"https://webhooks.mongodb-stitch.com/api/client/v2.0/app/covid-19-qppza/service/REST-API/incoming_webhook/global?country_iso3={iso}&hide_fields=_id, country, country_code, country_iso2, country_iso3, loc, state, uid");
            client.Timeout = -1;
            var request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);

            //Console.WriteLine(response.Content);

            //var json = JsonSerializer.Deserialize<Root>(response.Content);
            var json = (JArray)JsonConvert.DeserializeObject(response.Content);

            var item1 = json.Select(item => DateTime.Parse(item["date"].ToString().Substring(0, 10).Replace("-", ""))).Cast<DateTime>().ToArray();
            var item2 = json.Select(item => (int)item["confirmed_daily"]).Cast<int>().ToArray();

            Plot(item1, item2);
        }

        private void Plot(DateTime[] dates, int[] values)
        {
            chart1.Series.Clear();
            chart1.ChartAreas.Clear();

            var chartArea1 = "Area1";
            chart1.ChartAreas.Add(new System.Windows.Forms.DataVisualization.Charting.ChartArea(chartArea1));
            var legentd1 = "Graph1";
            chart1.Series.Add(legentd1);
            chart1.Series[legentd1].ChartType = SeriesChartType.Line;

            for(int i =0;i< dates.Length;i++)
            {
                //chart1.Series[legentd1].Points.AddY(values[i]);
                chart1.Series[legentd1].Points.AddXY(dates[i], Math.Max(0,values[i]));
            }
        }
    }
}
