using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Data.SqlClient;
using MaxMind.Db;
using MaxMind.GeoIP2;

namespace LogParser
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
		}
		DataSet ds = new DataSet();
		Random rnd = new Random();
		private void button1_Click(object sender, EventArgs e)
		{
			string Костыль = "Log" + rnd.Next(1, 1000000).ToString();
			if (textBox1.Text == "" && textBox2.Text == "")
				MessageBox.Show("Путь пуст:(");
			else
			{
				StreamReader sr = new StreamReader(textBox1.Text);
				ds = new DataSet();
				ds.Tables.Add("Logs");
				ds.Tables[0].Columns.Add("1");
				ds.Tables[0].Columns.Add("2");
				ds.Tables[0].Columns.Add("3");
				ds.Tables[0].Columns.Add("4");
				ds.Tables[0].Columns.Add("5");
				ds.Tables[0].Columns.Add("6");
				ds.Tables[0].Columns.Add("7");
				ds.Tables[0].Columns.Add("Dates");
				ds.Tables[0].Columns.Add("Times");
				ds.Tables[0].Columns.Add("Keys");
				ds.Tables[0].Columns.Add("11");
				ds.Tables[0].Columns.Add("IP");
				ds.Tables[0].Columns.Add("URL");
				ds.Tables[0].Columns.Add("Country");
				string row1 = sr.ReadLine();
				while (row1 != null)
				{
					string[] val = System.Text.RegularExpressions.Regex.Split(row1, " ");
					ds.Tables[0].Rows.Add(val);
					row1 = sr.ReadLine();
				}
				dataGridView1.DataSource = ds.Tables[0];
				for (int i = 1; i < 8; i++)
					dataGridView1.Columns.Remove(i.ToString());
				dataGridView1.Columns.Remove("11");
				using (var reader = new DatabaseReader("GeoLite2-Country.mmdb"))
				{
					foreach (DataGridViewRow row in dataGridView1.Rows)
					{
						try
						{
							string a = row.Cells[3].Value.ToString();
							var city = reader.Country(a);
							row.Cells[5].Value = city.Country.Name;
						}
						catch
						{
							row.Cells[5].Value = "Unknown country";
						}
					}
				}
				String str;
				SqlConnection myConn = new SqlConnection(" Data Source = (LocalDB)\\MSSQLLocalDB;Integrated Security = True;  Connect Timeout = 30;database=master");
				str = "CREATE DATABASE " + Костыль + " ON PRIMARY " +
					"(NAME = MyDatabase_Data, " +
					"FILENAME ='" + textBox2.Text + ".mdf', " +
					"SIZE = 3MB, MAXSIZE = 10MB, FILEGROWTH = 10%) " +
					"LOG ON (NAME = MyDatabase_Log, " +
					"FILENAME ='" + textBox2.Text + ".ldf', " +
					"SIZE = 1MB, " +
					"MAXSIZE = 5MB, " +
					"FILEGROWTH = 10%)";
				SqlCommand myCommand = new SqlCommand(str, myConn);
				try
				{
					myConn.Open();
					myCommand.ExecuteNonQuery();
				}
				catch
				{ 
					System.IO.File.Delete(@textBox2.Text + ".mdf");
					System.IO.File.Delete(@textBox2.Text + ".ldf");
				}
				finally
				{
					if (myConn.State == ConnectionState.Open)
					{
						myConn.Close();
					}
				}
				str = "create table Logs(Dates Date not null, Times TIME not null, Keys varchar(max) not null, IP varchar(max) not null, URL varchar(max) not null , Country varchar(max))";
				SqlConnection myConn1 = new SqlConnection("Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=" + textBox2.Text + ".mdf;Integrated Security=True;Connect Timeout=30");
				SqlCommand myCommand1 = new SqlCommand(str, myConn1);
				str = "INSERT INTO Logs (Dates, Times, Keys, IP, URL,Country) VALUES(@Dates, @Times, @Keys, @IP,@URL,@Country)";
				SqlCommand sc = new SqlCommand(str, myConn1);
				try
				{
					myConn1.Open();
					myCommand1.ExecuteNonQuery();
					foreach (DataRow row in ds.Tables[0].Rows)
					{
						sc.Parameters.Clear();
						sc.Parameters.AddWithValue("@Dates", row["Dates"]);
						sc.Parameters.AddWithValue("@Times", row["Times"]);
						sc.Parameters.AddWithValue("@Keys", row["Keys"]);
						sc.Parameters.AddWithValue("@IP", row["IP"]);
						sc.Parameters.AddWithValue("@URL", row["URL"]);
						sc.Parameters.AddWithValue("@Country", row["Country"]);
						sc.ExecuteNonQuery();
					}
					myConn1.Close();
					MessageBox.Show("База MS SQL создана");
				}
				catch { MessageBox.Show("Таблица не создана"); }
			}
		}



		private void button3_Click(object sender, EventArgs e)
		{
			string conString = "Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=" + textBox2.Text + ".mdf;Integrated Security=True;Connect Timeout=30";
			using (SqlConnection connection = new SqlConnection(conString))
			{
				connection.Open();
				using (SqlCommand command = new SqlCommand(
					"Select top 1 Country, count(*) as Counts from Logs group by Country order by Counts desc;",
					connection))
				{
					using (SqlDataReader reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							MessageBox.Show("Посетители из "+reader.GetString(0)+" совершают больше всего действий: "+(reader.GetInt32(1)).ToString());
						}
					}
				}
			}
		}

		private void button2_Click(object sender, EventArgs e)
		{
			if(comboBox1.SelectedItem!=null)
			{ 
				string conString = "Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=" + textBox2.Text + ".mdf;Integrated Security=True;Connect Timeout=30";
				using (SqlConnection connection = new SqlConnection(conString))
				{
					connection.Open();
					using (SqlCommand command = new SqlCommand(
						"Select top 1 Country, count(*) as Counts from Logs where URL like '%"+comboBox1.SelectedItem.ToString()+"%' group by Country order by Counts desc;",
						connection))
					{
						using (SqlDataReader reader = command.ExecuteReader())
						{
							while (reader.Read())
							{
								MessageBox.Show("Посетители из " + reader.GetString(0) + " чаще всего интересуются товарами из: " + comboBox1.SelectedItem.ToString());
							}
						}
					}
				}
			}
		}

		private void button4_Click(object sender, EventArgs e)
		{
			if (comboBox2.SelectedItem != null)
			{
				string conString = "Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=" + textBox2.Text + ".mdf;Integrated Security=True;Connect Timeout=30";
				using (SqlConnection connection = new SqlConnection(conString))
				{
					connection.Open();
					using (SqlCommand command = new SqlCommand(
						"select * from (select count(*) as AM from Logs where Times between '00:00:00' and '11:59:59' and URL like '%" + comboBox2.SelectedItem.ToString() + "%') as AM,(select count(*) as PM from Logs where Times between '12:00:00' and '23:59:59' and URL like  '%" + comboBox2.SelectedItem.ToString() + "%') as PM ",
						connection))
					{
						using (SqlDataReader reader = command.ExecuteReader())
						{
							while (reader.Read())
							{
							if(reader.GetInt32(0)> reader.GetInt32(1))
								MessageBox.Show("Посетители чаще просматриваю товары категории: "+ comboBox2.SelectedItem.ToString() + " до полудня " + reader.GetInt32(0) + " vs " + reader.GetInt32(1));
							else
									MessageBox.Show("Посетители чаще просматриваю товары категории: " + comboBox2.SelectedItem.ToString() + " до после полудня " + reader.GetInt32(0)+" vs "+ reader.GetInt32(1));
							}
						}
					}
				}
			}
		}
	}
}
