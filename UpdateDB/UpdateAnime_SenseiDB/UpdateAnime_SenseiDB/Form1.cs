using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UpdateAnime_SenseiDB
{
    public partial class Form1 : Form
    {
        static SqlConnection connection;
        static string connectionString = "Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=\"C:\\Users\\EugeniuCojocaru\\Desktop\\Limbaje formale si translatoare\\tema2\\DB\\anime.mdf\";Integrated Security=True;Connect Timeout=30";
        
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            using (connection = new SqlConnection(connectionString))
            using (SqlDataAdapter adapter = new SqlDataAdapter("SELECT * FROM Genre", connection))
            {
                connection.Open();
                DataTable table = new DataTable();
                adapter.Fill(table);

                genreList.DataSource = null;
                genreList.DataSource = table;
                genreList.DisplayMember = "Genre";
                genreList.CheckOnClick = true;

                
            }
        }

        void refresh_data_grid_view()
        {            
            using (connection = new SqlConnection(connectionString))
            using (SqlDataAdapter adapter = new SqlDataAdapter("SELECT * FROM Anime", connection))
            {
                connection.Open();
                DataTable table = new DataTable();
                adapter.Fill(table);

                dataGridView1.DataSource = table;
            }
        }
        private void add_Click(object sender, EventArgs e)
        {
            string title = textBox1.Text;
            string year = textBox2.Text;
            string score = textBox3.Text;
            string episodes = textBox4.Text;
            if (!title.Equals("") && !year.Equals("") && !score.Equals("") && !episodes.Equals(""))
            {
                string query_validate = "SELECT COUNT(*) as nb FROM Anime WHERE Title = '" + title + "'";
                string query_get_id = "SELECT Id FROM Anime WHERE Title = '" + title + "'";
                string query_insert_anime = "INSERT INTO Anime (Title,Year,Score,Episodes) VALUES ('" + title + "','" + year + "','" + score + "','" + episodes + "')";
                string query_insert_anime_genre = "INSERT INTO Anime_Genre (Id_anime,Id_genre) VALUES ('";
                using (connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand(query_validate, connection);
                    var reader = command.ExecuteReader();
                    if (reader.Read())
                        if (int.Parse(reader["nb"].ToString().Trim()) != 0)
                        {
                            MessageBox.Show("Anime already in db!");
                            return;
                        }
                        else
                        {
                            reader.Close();
                            command = new SqlCommand(query_insert_anime, connection);
                            command.ExecuteNonQuery();
                            command = new SqlCommand(query_get_id, connection);
                            reader = command.ExecuteReader();
                            if (reader.Read())
                            {
                                string id = reader["Id"].ToString().Trim();
                                if (genreList.SelectedItems.Count != 0)
                                {
                                    reader.Close();
                                    for (int i = 0; i < genreList.Items.Count; i++)
                                    {
                                        if (genreList.GetItemChecked(i))
                                        {
                                            string q = query_insert_anime_genre + id + "','" + (i+1).ToString() + "')";
                                            Console.WriteLine(q);
                                            command = new SqlCommand(q, connection);
                                            command.ExecuteNonQuery();

                                        }
                                    }
                                    MessageBox.Show("Anime " + title + " added successfully");
                                    refresh_data_grid_view();
                                }
                                else
                                    MessageBox.Show("Select genres from list");
                            }
                            else
                                MessageBox.Show("Can't find anime Id");
                        }


                }
            }
        }
    }
}
