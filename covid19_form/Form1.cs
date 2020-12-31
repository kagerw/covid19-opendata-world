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
            cbFunction.Items.Add("移動平均");

            cboIso.DataSource = GetMetaDatas("countries");
            //cboReason.DataSource = GetMetaDatas("states");
        }

        private void button1_Click(object sender, EventArgs e)
        {

            var country = cboIso.SelectedValue.ToString();

            var client = new RestClient($"https://webhooks.mongodb-stitch.com/api/client/v2.0/app/covid-19-qppza/service/REST-API/incoming_webhook/countries_summary?country={country}&min_date=2020-01-01T00:00:00.000Z&hide_fields=_id, country, country_code, country_iso2, country_iso3, loc, state");
            client.Timeout = -1;
            var request = new RestRequest(Method.GET);
            IRestResponse response = client.ExecuteAsync(request).Result;


            //Console.WriteLine(response.Content);

            //var json = JsonSerializer.Deserialize<Root>(response.Content);
            if (response.IsSuccessful)
            {
                var json = ConvertToJson<JArray>(response.Content);

                var item1 = json.Select(item => DateTime.Parse(item["date"].ToString().Substring(0, 10).Replace("-", ""))).Cast<DateTime>().ToArray();
                var item2 = SelectedFunction(json);

                if (item2 != null)
                {
                    ClearPlotArea();
                    Plot(cbFunction.SelectedItem.ToString(), item1.Skip(7).ToArray() , item2.Cast<double>().ToArray());
                    //Plot("移動平均",item1, MovingAverage(json.Select(item => (int)item["confirmed_daily"]).ToList(), 7));
                    Plot("実効再生産数", item1.Skip(7).ToArray(), Rt(json.Select(item => (int)item["confirmed_daily"]).ToList(), 7));
                }
            }
        }

        TResult ConvertToJson<TResult>(string rowData)=> (TResult)JsonConvert.DeserializeObject(rowData);


        string[] GetMetaDatas(string tag)
        {
            var client = new RestClient("https://webhooks.mongodb-stitch.com/api/client/v2.0/app/covid-19-qppza/service/REST-API/incoming_webhook/metadata");
            client.Timeout = -1;
            var request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);
            return ConvertToJson<JToken>(response.Content)[tag].Select(item => item.ToString()).ToArray();
        }

        double[] SelectedFunction(JArray json)
        {
            switch(cbFunction.SelectedIndex)
            {
                case 0:
                    //新規発生
                    return json?.Select(item => (double)item["confirmed_daily"])?.ToArray();
                case 1:
                    //現在入院患者
                    return json?.Select(item => (double)item["confirmed"] - (int)item["recovered"] - (int)item["deaths"]).ToArray();
                case 2:
                    //回復
                    return json?.Select(item => (double)item["recovered_daily"]).ToArray();
                case 3:
                    //移動平均
                    return MovingAverage(json.Select(item => (double)item["confirmed_daily"]).ToList(), 7);

            }
            return null;


        }
        /// <summary>
        /// 移動平均の計算
        /// </summary>
        /// <param name="data"></param>
        /// <param name="num"></param>
        /// <returns></returns>
        double[] MovingAverage(
            IList<double> data,
            int num)
        {
            return Enumerable.Range(0, data.Count() - num)
                .Select(i => (double)data.Skip(i - num).Take(num).Average())
                .ToArray();
        }

        /// <summary>
        /// 実効再生産数の計算
        /// </summary>
        /// <param name="data"></param>
        /// <param name="num"></param>
        /// <returns></returns>
        double[] Rt(
            IList<int> data,
            int num)
        {
            return Enumerable.Range(0, data.Count() - num)
                .Select(i => {
                    if (i < num)
                        return 1;

                    return Math.Pow((double)data.Skip(i).Take(num).Sum() / Math.Max(0.1, data.Skip(i - num).Take(num).Sum()), (double)5 / 7);
                    })
                .ToArray();
        }

        private void Plot(string areaName, DateTime[] dates, double[] values)
        {
            var chartArea1 = "test";
            if(!chart1.ChartAreas.Any(area => area.Name == chartArea1 ))
                chart1.ChartAreas.Add(new ChartArea(chartArea1));
            //if(areaName == "実効再生産数")
            //    chart1.ChartAreas.Add(new ChartArea(chartArea1)
            //    {
            //        Y
            //    });
            var legentd1 = areaName;
            chart1.Series.Add(legentd1);
            chart1.Series[legentd1].ChartType = SeriesChartType.Line;

            if (areaName == "実効再生産数")
            {
                chart1.Series[legentd1].YAxisType = AxisType.Secondary;
                chart1.ChartAreas.First().AxisY2.Maximum = 3;
            }

            for(int i =0;i< values.Length;i++)
            {
                //chart1.Series[legentd1].Points.AddY(values[i]);
                chart1.Series[legentd1].Points.AddXY(dates[i], Math.Max(0,values[i]));
            }
            chart1.Serializer.Save($"{areaName}.txt");
            chart1.Update();
        }

        private void ClearPlotArea()
        {
            chart1.Series.Clear();
            chart1.ChartAreas.Clear();
        }

    }
}
