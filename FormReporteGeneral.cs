using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace Arquimedes
{
    public partial class FormReporteGeneral : Form
    {
        public FormReporteGeneral()
        {
            InitializeComponent();
        }

        private void FormReporteGeneral_Load(object sender, EventArgs e)
        {
            lblMensaje.Visible = false;
            dtpInicio.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            dtpFin.Value = DateTime.Now;
            CargarGrupos();
        }

        private void CargarGrupos()
        {
            string connString = ConfigurationManager.ConnectionStrings["ArquimedesDB"].ConnectionString;
            using (var conn = new SqlConnection(connString))
            {
                conn.Open();
                string query = "SELECT DISTINCT Grado, Grupo FROM Alumnos ORDER BY Grado, Grupo";

                using (var cmd = new SqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    cmbGrupo.Items.Clear();
                    while (reader.Read())
                        cmbGrupo.Items.Add(reader["Grado"] + "° " + reader["Grupo"]);
                }
            }

            if (cmbGrupo.Items.Count > 0)
                cmbGrupo.SelectedIndex = 0;
        }

        private void btnConsultar_Click(object sender, EventArgs e)
        {
            if (cmbGrupo.SelectedItem == null)
            {
                MostrarMensaje("Selecciona un grupo.", true);
                return;
            }

            string[] partes = cmbGrupo.SelectedItem.ToString().Replace("°", "").Split(' ');
            string grado = partes[0];
            string grupo = partes[1];

            try
            {
                string connString = ConfigurationManager.ConnectionStrings["ArquimedesDB"].ConnectionString;
                using (var conn = new SqlConnection(connString))
                {
                    conn.Open();
                    string query = @"
                        SELECT 
                            a.Id,
                            a.Nombre + ' ' + a.Apellido AS Alumno,
                            ISNULL(asis.Presentes, 0) AS Presentes,
                            ISNULL(asis.Faltas, 0) AS Faltas,
                            ISNULL(col.Adeudos, 0) AS ColegiaturasPendientes,
                            col.UltimoPago
                        FROM Alumnos a
                        LEFT JOIN (
                            SELECT AlumnoId,
                                SUM(CASE WHEN Estatus = 'Presente' THEN 1 ELSE 0 END) AS Presentes,
                                SUM(CASE WHEN Estatus = 'Falta' THEN 1 ELSE 0 END) AS Faltas
                            FROM Asistencias2
                            WHERE Fecha BETWEEN @FechaInicio AND @FechaFin
                            GROUP BY AlumnoId
                        ) asis ON asis.AlumnoId = a.Id
                        LEFT JOIN (
                            SELECT AlumnoId,
                                SUM(CASE WHEN Estatus IN ('Pendiente','Vencido') THEN 1 ELSE 0 END) AS Adeudos,
                                MAX(FechaPago) AS UltimoPago
                            FROM Colegiaturas
                            GROUP BY AlumnoId
                        ) col ON col.AlumnoId = a.Id
                        WHERE a.Grado = @Grado AND a.Grupo = @Grupo
                        ORDER BY a.Apellido";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@FechaInicio", dtpInicio.Value.Date);
                        cmd.Parameters.AddWithValue("@FechaFin", dtpFin.Value.Date);
                        cmd.Parameters.AddWithValue("@Grado", grado);
                        cmd.Parameters.AddWithValue("@Grupo", grupo);

                        using (var adapter = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);
                            dgvReporte.DataSource = dt;
                        }
                    }
                }

                ResaltarAdeudos();
                MostrarMensaje("Reporte generado correctamente.", false);
            }
            catch (Exception ex)
            {
                MostrarMensaje("Error al generar el reporte: " + ex.Message, true);
            }
        }

        private void ResaltarAdeudos()
        {
            foreach (DataGridViewRow fila in dgvReporte.Rows)
            {
                if (fila.Cells["ColegiaturasPendientes"].Value != null &&
                    Convert.ToInt32(fila.Cells["ColegiaturasPendientes"].Value) > 0)
                {
                    fila.DefaultCellStyle.BackColor = Color.MistyRose;
                }
            }
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            lblMensaje.Text = mensaje;
            lblMensaje.ForeColor = esError ? Color.Red : Color.Green;
            lblMensaje.Visible = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Form4 reporte = new Form4();
            reporte.Show();
            this.Hide();
        }
    }
}