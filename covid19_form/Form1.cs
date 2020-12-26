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
        delegate int[] calclationFunc(JArray json);
        public Form1()
        {
            InitializeComponent();
            cbFunction.Items.Add("新規発生");
            cbFunction.Items.Add("現在患者");
            cbFunction.Items.Add("回復");

            cboIso.DataSource = GetMetaDatas();
        }

        private void button1_Click(object sender, EventArgs e)
        {

            var country = cboIso.SelectedValue.ToString();

            var client = new RestClient($"https://webhooks.mongodb-stitch.com/api/client/v2.0/app/covid-19-qppza/service/REST-API/incoming_webhook/global_and_us?country={country}&min_date=2020-01-01T00:00:00.000Z&hide_fields=_id, country, country_code, country_iso2, country_iso3, loc, state");
            client.Timeout = -1;
            var request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);


            //Console.WriteLine(response.Content);

            //var json = JsonSerializer.Deserialize<Root>(response.Content);
            if (response.IsSuccessful)
            {
                var json = ConvertToJson<JArray>(response.Content);

                var item1 = json.Select(item => DateTime.Parse(item["date"].ToString().Substring(0, 10).Replace("-", ""))).Cast<DateTime>().ToArray();
                var item2 = SelectedFunction(json)?.Invoke(json);

                if (item2 != null)
                    Plot(item1, item2.ToArray());
            }
        }

        TResult ConvertToJson<TResult>(string rowData)=> (TResult)JsonConvert.DeserializeObject(rowData);


        string[] GetMetaDatas()
        {
            var client = new RestClient("https://webhooks.mongodb-stitch.com/api/client/v2.0/app/covid-19-qppza/service/REST-API/incoming_webhook/metadata");
            client.Timeout = -1;
            var request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);
            return ConvertToJson<JToken>(response.Content)["countries"].Select(item => item.ToString()).ToArray();
        }

        calclationFunc SelectedFunction(JArray json)
        {
            switch(cbFunction.SelectedIndex)
            {
                case 0:
                    //新規発生
                    return array => json?.Select(item => (int)item["confirmed_daily"])?.ToArray();
                case 1:
                    //現在入院患者
                    return array => json?.Select(item => (int)item["confirmed"] - (int)item["recovered"] - (int)item["deaths"]).ToArray();
                case 2:
                    //回復
                    return array => json?.Select(item => (int)item["recovered_daily"]).ToArray();

            }
            return null;
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
