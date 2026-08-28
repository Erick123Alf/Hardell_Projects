using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using iTextFont = iTextSharp.text.Font;

namespace Arquimedes
{
    public partial class Form6 : Form
    {
        public Form6()
        {
            InitializeComponent();
        }

        private void Form6_Load(object sender, EventArgs e)
        {
            lblMens.Visible = false;
            dtpFecha.Value = DateTime.Now;
            CargarGrupos(); // dispara SelectedIndexChanged -> CargarAsistenciaSemanal()
        }

        private DateTime ObtenerLunesDeLaSemana(DateTime fecha)
        {
            int diasHastaLunes = ((int)fecha.DayOfWeek == 0) ? 6 : (int)fecha.DayOfWeek - 1;
            return fecha.AddDays(-diasHastaLunes).Date;
        }

        private void ConfigurarGridSemanal(DateTime lunes)
        {
            dgvAsistencia.Columns.Clear();
            dgvAsistencia.Columns.Add("AlumnoId", "Id");
            dgvAsistencia.Columns["AlumnoId"].Visible = false;
            dgvAsistencia.Columns.Add("Nombre", "Alumno");
            dgvAsistencia.Columns["Nombre"].ReadOnly = true;

            string[] nombresDias = { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes" };

            for (int i = 0; i < 5; i++)
            {
                var col = new DataGridViewCheckBoxColumn();
                col.Name = "Dia" + i;
                col.HeaderText = nombresDias[i] + "\n" + lunes.AddDays(i).ToString("dd/MM");
                dgvAsistencia.Columns.Add(col);
            }

            dgvAsistencia.AllowUserToAddRows = false;
            dgvAsistencia.ColumnHeadersHeight = 40;
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
                    {
                        string grado = reader["Grado"].ToString();
                        string grupo = reader["Grupo"].ToString();
                        cmbGrupo.Items.Add(grado + "° " + grupo);
                    }
                }
            }

            if (cmbGrupo.Items.Count > 0)
                cmbGrupo.SelectedIndex = 0;
        }

        private void cmbGrupo_SelectedIndexChanged(object sender, EventArgs e)
        {
            CargarAsistenciaSemanal();
        }

        private void dtpFecha_ValueChanged(object sender, EventArgs e)
        {
            CargarAsistenciaSemanal();
        }

        private void CargarAsistenciaSemanal()
        {
            if (cmbGrupo.SelectedItem == null) return;

            string[] partes = cmbGrupo.SelectedItem.ToString().Replace("°", "").Split(' ');
            string grado = partes[0];
            string grupo = partes[1];

            DateTime lunes = ObtenerLunesDeLaSemana(dtpFecha.Value);
            DateTime viernes = lunes.AddDays(4);

            ConfigurarGridSemanal(lunes);
            dgvAsistencia.Rows.Clear();

            try
            {
                string connString = ConfigurationManager.ConnectionStrings["ArquimedesDB"].ConnectionString;
                using (var conn = new SqlConnection(connString))
                {
                    conn.Open();

                    string queryAlumnos = "SELECT Id, Nombre, Apellido FROM Alumnos WHERE Grado = @Grado AND Grupo = @Grupo ORDER BY Apellido";
                    DataTable dtAlumnos = new DataTable();
                    using (var cmd = new SqlCommand(queryAlumnos, conn))
                    {
                        cmd.Parameters.AddWithValue("@Grado", grado);
                        cmd.Parameters.AddWithValue("@Grupo", grupo);
                        using (var adapter = new SqlDataAdapter(cmd))
                            adapter.Fill(dtAlumnos);
                    }

                    string queryAsist = "SELECT AlumnoId, Fecha, Estatus FROM Asistencias2 WHERE Fecha BETWEEN @Inicio AND @Fin";
                    DataTable dtAsist = new DataTable();
                    using (var cmd = new SqlCommand(queryAsist, conn))
                    {
                        cmd.Parameters.AddWithValue("@Inicio", lunes);
                        cmd.Parameters.AddWithValue("@Fin", viernes);
                        using (var adapter = new SqlDataAdapter(cmd))
                            adapter.Fill(dtAsist);
                    }

                    foreach (DataRow alumno in dtAlumnos.Rows)
                    {
                        int id = Convert.ToInt32(alumno["Id"]);
                        string nombreCompleto = alumno["Nombre"] + " " + alumno["Apellido"];

                        object[] valoresFila = new object[7];
                        valoresFila[0] = id;
                        valoresFila[1] = nombreCompleto;

                        for (int i = 0; i < 5; i++)
                        {
                            DateTime diaActual = lunes.AddDays(i);
                            bool presente = true;

                            foreach (DataRow reg in dtAsist.Rows)
                            {
                                if (Convert.ToInt32(reg["AlumnoId"]) == id &&
                                    Convert.ToDateTime(reg["Fecha"]).Date == diaActual.Date)
                                {
                                    presente = reg["Estatus"].ToString() == "Presente";
                                    break;
                                }
                            }

                            valoresFila[2 + i] = presente;
                        }

                        dgvAsistencia.Rows.Add(valoresFila);
                    }
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje("Error al cargar la semana: " + ex.Message, true);
            }
        }

        private void btnGuardar_Click(object sender, EventArgs e)
        {
            if (dgvAsistencia.Rows.Count == 0)
            {
                MostrarMensaje("No hay alumnos cargados.", true);
                return;
            }

            DateTime lunes = ObtenerLunesDeLaSemana(dtpFecha.Value);

            try
            {
                string connString = ConfigurationManager.ConnectionStrings["ArquimedesDB"].ConnectionString;
                using (var conn = new SqlConnection(connString))
                {
                    conn.Open();

                    foreach (DataGridViewRow fila in dgvAsistencia.Rows)
                    {
                        if (fila.IsNewRow) continue;

                        int alumnoId = Convert.ToInt32(fila.Cells["AlumnoId"].Value);

                        for (int i = 0; i < 5; i++)
                        {
                            DateTime diaActual = lunes.AddDays(i);
                            bool presente = Convert.ToBoolean(fila.Cells["Dia" + i].Value);
                            string estatus = presente ? "Presente" : "Falta";

                            string query = @"
                                MERGE Asistencias2 AS destino
                                USING (SELECT @AlumnoId AS AlumnoId, @Fecha AS Fecha) AS origen
                                ON destino.AlumnoId = origen.AlumnoId AND destino.Fecha = origen.Fecha
                                WHEN MATCHED THEN
                                    UPDATE SET Estatus = @Estatus
                                WHEN NOT MATCHED THEN
                                    INSERT (AlumnoId, Fecha, Estatus)
                                    VALUES (@AlumnoId, @Fecha, @Estatus);";

                            using (var cmd = new SqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@AlumnoId", alumnoId);
                                cmd.Parameters.AddWithValue("@Fecha", diaActual);
                                cmd.Parameters.AddWithValue("@Estatus", estatus);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                }

                MostrarMensaje("Asistencia semanal guardada correctamente.", false);
            }
            catch (Exception ex)
            {
                MostrarMensaje("Error al guardar: " + ex.Message, true);
            }
        }

        private void btnExportarPDF_Click(object sender, EventArgs e)
        {
            if (dgvAsistencia.Rows.Count == 0)
            {
                MostrarMensaje("No hay datos para exportar.", true);
                return;
            }

            using (SaveFileDialog dialogo = new SaveFileDialog())
            {
                dialogo.Filter = "Archivo PDF (*.pdf)|*.pdf";
                dialogo.FileName = "Asistencia_" + cmbGrupo.SelectedItem + "_" +
                    ObtenerLunesDeLaSemana(dtpFecha.Value).ToString("dd-MM-yyyy") + ".pdf";

                if (dialogo.ShowDialog() != DialogResult.OK)
                    return;

                try
                {
                    GenerarPDFAsistencia(dialogo.FileName);
                    MostrarMensaje("PDF generado correctamente.", false);
                    System.Diagnostics.Process.Start(dialogo.FileName);
                }
                catch (Exception ex)
                {
                    MostrarMensaje("Error al generar PDF: " + ex.Message, true);
                }
            }
        }

        private void GenerarPDFAsistencia(string rutaArchivo)
        {
            Document documento = new Document(PageSize.A4, 30, 30, 30, 30);
            PdfWriter.GetInstance(documento, new FileStream(rutaArchivo, FileMode.Create));
            documento.Open();

            iTextFont fuenteTitulo = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
            Paragraph titulo = new Paragraph("Instituto Educativo Arquimedes", fuenteTitulo);
            titulo.Alignment = Element.ALIGN_CENTER;
            documento.Add(titulo);

            iTextFont fuenteSubtitulo = FontFactory.GetFont(FontFactory.HELVETICA, 12);
            DateTime lunes = ObtenerLunesDeLaSemana(dtpFecha.Value);
            DateTime viernes = lunes.AddDays(4);

            Paragraph subtitulo = new Paragraph(
                $"Reporte de Asistencia - Grupo {cmbGrupo.SelectedItem}\n" +
                $"Semana del {lunes:dd/MM/yyyy} al {viernes:dd/MM/yyyy}",
                fuenteSubtitulo);
            subtitulo.Alignment = Element.ALIGN_CENTER;
            subtitulo.SpacingAfter = 15f;
            documento.Add(subtitulo);

            PdfPTable tabla = new PdfPTable(6);
            tabla.WidthPercentage = 100;
            tabla.SetWidths(new float[] { 3f, 1f, 1f, 1f, 1f, 1f });

            iTextFont fuenteHeader = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, iTextSharp.text.BaseColor.WHITE);
            string[] encabezados = { "Alumno", "Lun", "Mar", "Mié", "Jue", "Vie" };

            foreach (string encabezado in encabezados)
            {
                PdfPCell celda = new PdfPCell(new Phrase(encabezado, fuenteHeader));
                celda.BackgroundColor = new iTextSharp.text.BaseColor(74, 111, 165);
                celda.HorizontalAlignment = Element.ALIGN_CENTER;
                celda.Padding = 6;
                tabla.AddCell(celda);
            }

            iTextFont fuenteCelda = FontFactory.GetFont(FontFactory.HELVETICA, 10);

            foreach (DataGridViewRow fila in dgvAsistencia.Rows)
            {
                if (fila.IsNewRow) continue;

                string nombre = fila.Cells["Nombre"].Value.ToString();
                PdfPCell celdaNombre = new PdfPCell(new Phrase(nombre, fuenteCelda));
                celdaNombre.Padding = 5;
                tabla.AddCell(celdaNombre);

                for (int i = 0; i < 5; i++)
                {
                    bool presente = Convert.ToBoolean(fila.Cells["Dia" + i].Value);
                    string texto = presente ? "Si" : "No";

                    PdfPCell celdaDia = new PdfPCell(new Phrase(texto, fuenteCelda));
                    celdaDia.HorizontalAlignment = Element.ALIGN_CENTER;
                    celdaDia.Padding = 5;
                    celdaDia.BackgroundColor = presente
                        ? new iTextSharp.text.BaseColor(220, 245, 220)
                        : new iTextSharp.text.BaseColor(250, 220, 220);
                    tabla.AddCell(celdaDia);
                }
            }

            documento.Add(tabla);

            Paragraph pie = new Paragraph(
                "\nGenerado el " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 8));
            pie.Alignment = Element.ALIGN_RIGHT;
            documento.Add(pie);

            documento.Close();
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            lblMens.Text = mensaje;
            lblMens.ForeColor = esError ? Color.Red : Color.Green;
            lblMens.Visible = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Form4 form4 = new Form4();
            form4.Show();
            this.Hide();
        }
    }
}