using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace Arquimedes
{
    public partial class FormAsistencia : Form
    {
        private string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=ArquimedesDB;Integrated Security=True;";
        private Timer timerReset;

        public FormAsistencia()
        {
            InitializeComponent();

            timerReset = new Timer();
            timerReset.Interval = 4000;
            timerReset.Tick += TimerReset_Tick;
        }

        private void FormAsistencia_Load(object sender, EventArgs e)
        {
            ResetearPantalla();
        }

        private void btnBuscar_Click(object sender, EventArgs e)
        {
            EjecutarBusqueda();
        }

        private void pictureBoxBuscar_Click(object sender, EventArgs e)
        {
            EjecutarBusqueda();
        }

        private void txtScan_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                EjecutarBusqueda();
            }
        }

        private void EjecutarBusqueda()
        {
            string codigo = txtScan.Text.Trim();

            if (string.IsNullOrEmpty(codigo))
            {
                MessageBox.Show("Por favor ingresa o escanea la matrícula o clave (Grado+Grupo+N°Lista).", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtScan.Focus();
                return;
            }

            ProcesarEscaneo(codigo);
        }

        private void ProcesarEscaneo(string codigo)
        {
            timerReset.Stop();

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    DateTime horaActual = DateTime.Now;
                    string mesActual = horaActual.ToString("MMMM", new System.Globalization.CultureInfo("es-ES"));
                    int anioActual = horaActual.Year;

                    // Consulta adaptada a las columnas de tu CREATE TABLE [dbo].[Colegiaturas]
                    string sql = @"
                        SELECT a.Id, a.Matricula, a.Nombre, a.Apellido, a.Grado, a.Grupo, a.NumLista,
                               c.Estatus AS EstatusColegiatura
                        FROM Alumnos a
                        LEFT JOIN Colegiaturas c 
                               ON a.Id = c.AlumnoId 
                              AND LOWER(c.Mes) = LOWER(@MesActual)
                              AND c.Anio = @AnioActual
                        WHERE a.Matricula = @Cod 
                           OR CAST(a.Id AS NVARCHAR) = @Cod 
                           OR (CONCAT(a.Grado, a.Grupo, a.NumLista) = @Cod)";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Cod", codigo);
                        cmd.Parameters.AddWithValue("@MesActual", mesActual);
                        cmd.Parameters.AddWithValue("@AnioActual", anioActual);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int idAlumno = Convert.ToInt32(reader["Id"]);
                                string matricula = reader["Matricula"]?.ToString() ?? "";
                                string nombreCompleto = $"{reader["Nombre"]} {reader["Apellido"]}";
                                string grado = reader["Grado"]?.ToString() ?? "";
                                string grupo = reader["Grupo"]?.ToString() ?? "";
                                string numLista = reader["NumLista"]?.ToString() ?? "";

                                // Leer el campo 'Estatus' de la tabla Colegiaturas ('Pagado', 'Pendiente', 'Vencido')
                                string estatusColegiatura = reader["EstatusColegiatura"]?.ToString() ?? "Pendiente";
                                bool estaPagado = estatusColegiatura.Equals("Pagado", StringComparison.OrdinalIgnoreCase);

                                reader.Close();

                                if (!estaPagado)
                                {
                                    // Bloqueo de Asistencia si el Estatus es 'Pendiente', 'Vencido' o no existe registro
                                    lblMensaje.ForeColor = Color.DarkRed;
                                    lblMensaje.Text = $"❌ COLEGIATURA DE {mesActual.ToUpper()} ({estatusColegiatura.ToUpper()})";
                                    System.Media.SystemSounds.Hand.Play();
                                }
                                else
                                {
                                    // Registrar Asistencia exitosa en la tabla Asistencias
                                    RegistrarAsistencia(conn, idAlumno, matricula, nombreCompleto, grado, grupo, horaActual);

                                    lblMensaje.ForeColor = Color.DarkGreen;
                                    lblMensaje.Text = "✅ ¡ASISTENCIA REGISTRADA!";
                                    System.Media.SystemSounds.Asterisk.Play();
                                }

                                // Desplegar datos en los Labels de la interfaz
                                if (lblInfoAlumno != null)
                                {
                                    lblInfoAlumno.Text = $"Alumno: {nombreCompleto}\n" +
                                                         $"Grado: {grado} | Grupo: {grupo} | N° Lista: {numLista}\n" +
                                                         $"Estatus Mes ({mesActual}): {estatusColegiatura}";
                                    lblInfoAlumno.Visible = true;
                                }

                                if (lblFecha != null)
                                {
                                    lblFecha.Text = $"Ingreso: {horaActual:dd/MM/yyyy - hh:mm:ss tt}";
                                    lblFecha.Visible = true;
                                }
                            }
                            else
                            {
                                lblMensaje.ForeColor = Color.DarkRed;
                                lblMensaje.Text = $"❌ CÓDIGO/MATRÍCULA '{codigo}' NO ENCONTRADO";

                                if (lblInfoAlumno != null)
                                {
                                    lblInfoAlumno.Text = "";
                                    lblInfoAlumno.Visible = false;
                                }

                                if (lblFecha != null)
                                {
                                    lblFecha.Text = "";
                                    lblFecha.Visible = false;
                                }

                                System.Media.SystemSounds.Hand.Play();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al consultar la Base de Datos:\n\n" + ex.Message,
                    "Error SQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            timerReset.Start();
        }

        private void RegistrarAsistencia(SqlConnection conn, int idAlumno, string matricula, string nombre, string grado, string grupo, DateTime fechaHora)
        {
            string sqlInsert = "INSERT INTO Asistencias (IdAlumno, Matricula, Nombre, Grado, Grupo, Fecha, Hora) " +
                               "VALUES (@IdAlumno, @Matricula, @Nombre, @Grado, @Grupo, @Fecha, @Hora)";

            using (SqlCommand cmdInsert = new SqlCommand(sqlInsert, conn))
            {
                cmdInsert.Parameters.AddWithValue("@IdAlumno", idAlumno);
                cmdInsert.Parameters.AddWithValue("@Matricula", matricula);
                cmdInsert.Parameters.AddWithValue("@Nombre", nombre);
                cmdInsert.Parameters.AddWithValue("@Grado", grado);
                cmdInsert.Parameters.AddWithValue("@Grupo", grupo);
                cmdInsert.Parameters.AddWithValue("@Fecha", fechaHora.Date);
                cmdInsert.Parameters.AddWithValue("@Hora", fechaHora.TimeOfDay);

                cmdInsert.ExecuteNonQuery();
            }
        }

        private void TimerReset_Tick(object sender, EventArgs e)
        {
            timerReset.Stop();
            ResetearPantalla();
        }

        private void ResetearPantalla()
        {
            if (pictureBoxFoto != null)
            {
                pictureBoxFoto.Image = null;
                pictureBoxFoto.Visible = true;
            }

            lblMensaje.ForeColor = Color.DimGray;
            lblMensaje.Text = "Esperando Escaneo / Entrada Manual...";

            if (lblInfoAlumno != null)
            {
                lblInfoAlumno.Text = "";
                lblInfoAlumno.Visible = false;
            }

            if (lblFecha != null)
            {
                lblFecha.Text = "";
                lblFecha.Visible = false;
            }

            txtScan.Clear();
            txtScan.Focus();
        }

        private void btnCerrar_Click(object sender, EventArgs e)
        {   
            Form1 frm = new Form1();
            frm.Show();
            this.Close();
        }
    }
}