using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using iPDF = iTextSharp.text;

namespace Arquimedes
{
    public partial class FormColegiaturas : Form
    {
        public FormColegiaturas()
        {
            InitializeComponent();

            // --- CARGA FORZADA EN EL CONSTRUCTOR ---

            // 1. Meses
            cmbMes.Items.Clear();
            cmbMes.Items.AddRange(new object[] {
                "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
                "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"
            });
            if (cmbMes.Items.Count > 0) cmbMes.SelectedIndex = 0;

            // 2. Estatus
            cmbEstatus.Items.Clear();
            cmbEstatus.Items.AddRange(new object[] { "Pendiente", "Pagado", "Vencido" });
            if (cmbEstatus.Items.Count > 0) cmbEstatus.SelectedIndex = 0;

            // 3. Métodos de Pago
            cmbMetodoPago.Items.Clear();
            cmbMetodoPago.Items.AddRange(new object[] { "Efectivo", "Transferencia", "Tarjeta", "Depósito Bancario" });
            cmbMetodoPago.SelectedIndex = -1;
        }

        public static class GeneradorPDF
        {
            public static void GenerarComprobanteColegiatura(string matricula, string nombreAlumno, string concepto, string mes, string anio, string monto, string estatus)
            {
                try
                {
                    // Validar que exista al menos la matrícula o datos mínimos
                    if (string.IsNullOrWhiteSpace(matricula))
                    {
                        MessageBox.Show("Por favor, ingresa o selecciona la matrícula del alumno para generar el comprobante.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Definir ruta y nombre del archivo PDF
                    string carpeta = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "InstitutoArquimedes");
                    if (!Directory.Exists(carpeta))
                    {
                        Directory.CreateDirectory(carpeta);
                    }

                    string nombreArchivo = $"Comprobante_{matricula}_{mes}_{anio}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                    string rutaCompleta = Path.Combine(carpeta, nombreArchivo);

                    // Crear documento PDF
                    Document doc = new Document(PageSize.A4, 40, 40, 40, 40);
                    PdfWriter.GetInstance(doc, new FileStream(rutaCompleta, FileMode.Create));

                    doc.Open();

                    // Estilos y Fuentes (usando iTextSharp.text.Font explícito para evitar conflictos con System.Drawing.Font)
                    iTextSharp.text.Font fontTitulo = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, BaseColor.DARK_GRAY);
                    iTextSharp.text.Font fontSub = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.GRAY);
                    iTextSharp.text.Font fontNegrita = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, BaseColor.BLACK);
                    iTextSharp.text.Font fontNormal = FontFactory.GetFont(FontFactory.HELVETICA, 11, BaseColor.DARK_GRAY);

                    // Encabezado
                    Paragraph titulo = new Paragraph("INSTITUTO ARQUÍMEDES", fontTitulo);
                    titulo.Alignment = Element.ALIGN_CENTER;
                    doc.Add(titulo);

                    Paragraph subtitulo = new Paragraph("Comprobante de Pago de Colegiatura", fontSub);
                    subtitulo.Alignment = Element.ALIGN_CENTER;
                    doc.Add(subtitulo);

                    doc.Add(new Paragraph(" ")); // Espacio en blanco
                    doc.Add(new Chunk(new iTextSharp.text.pdf.draw.LineSeparator(0.5f, 100f, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER, -1)));
                    doc.Add(new Paragraph(" "));

                    // Información General
                    PdfPTable tablaInfo = new PdfPTable(2);
                    tablaInfo.WidthPercentage = 100;
                    tablaInfo.SetWidths(new float[] { 30f, 70f });

                    AgregarFilaTabla(tablaInfo, "Fecha de Emisión:", DateTime.Now.ToString("dd/MM/yyyy HH:mm"), fontNegrita, fontNormal);
                    AgregarFilaTabla(tablaInfo, "Matrícula:", matricula, fontNegrita, fontNormal);
                    AgregarFilaTabla(tablaInfo, "Alumno:", nombreAlumno, fontNegrita, fontNormal);
                    AgregarFilaTabla(tablaInfo, "Concepto:", concepto, fontNegrita, fontNormal);
                    AgregarFilaTabla(tablaInfo, "Periodo:", $"{mes} {anio}", fontNegrita, fontNormal);
                    AgregarFilaTabla(tablaInfo, "Monto:", $"${monto}", fontNegrita, fontNormal);
                    AgregarFilaTabla(tablaInfo, "Estatus:", estatus, fontNegrita, fontNormal);

                    doc.Add(tablaInfo);

                    doc.Add(new Paragraph(" "));
                    doc.Add(new Paragraph(" "));

                    // Pie de página / Aviso
                    Paragraph footer = new Paragraph("Este documento es un comprobante digital generado por el sistema de gestión del Instituto Arquímedes.", fontSub);
                    footer.Alignment = Element.ALIGN_CENTER;
                    doc.Add(footer);

                    doc.Close();

                    // Abrir el PDF automáticamente
                    var resultado = MessageBox.Show("¡PDF generado con éxito!\n\n¿Deseas abrir el archivo ahora?", "Comprobante Creado", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                    if (resultado == DialogResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(rutaCompleta) { UseShellExecute = true });
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al generar el archivo PDF: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            private static void AgregarFilaTabla(PdfPTable tabla, string etiqueta, string valor, iTextSharp.text.Font fontEtiqueta, iTextSharp.text.Font fontValor)
            {
                PdfPCell celdaEtiqueta = new PdfPCell(new Phrase(etiqueta, fontEtiqueta));
                celdaEtiqueta.Border = iTextSharp.text.Rectangle.NO_BORDER; // <-- Solucionado aquí
                celdaEtiqueta.PaddingBottom = 8;
                tabla.AddCell(celdaEtiqueta);

                PdfPCell celdaValor = new PdfPCell(new Phrase(valor, fontValor));
                celdaValor.Border = iTextSharp.text.Rectangle.NO_BORDER; // <-- Solucionado aquí
                celdaValor.PaddingBottom = 8;
                tabla.AddCell(celdaValor);
            }
        }

        private void FormColegiaturas_Load(object sender, EventArgs e)
        {
            lblMensaje.Visible = false;

            // Poner el año actual por defecto en el TextBox
            if (txtAnio != null && string.IsNullOrWhiteSpace(txtAnio.Text))
            {
                txtAnio.Text = DateTime.Now.Year.ToString();
            }

            if (dtpVencimiento != null)
            {
                dtpVencimiento.Value = DateTime.Now;
            }

            CargarColegiaturas();
        }

        private void CargarColegiaturas()
        {
            try
            {
                string connString = ConfigurationManager.ConnectionStrings["ArquimedesDB"].ConnectionString;
                using (var conn = new SqlConnection(connString))
                {
                    conn.Open();
                    // Usamos LEFT JOIN para evitar que la tabla quede en blanco si algún registro no encuentra el match exacto
                    string query = @"
                        SELECT c.Id, 
                               ISNULL(CONCAT(a.Matricula, ' - ', a.Nombre, ' ', a.Apellido), 'Sin Asignar') AS Alumno, 
                               c.Concepto, c.Mes, c.Anio, c.Monto, 
                               c.FechaVencimiento, c.Estatus, c.MetodoPago, c.FechaPago
                        FROM Colegiaturas c
                        LEFT JOIN Alumnos a ON c.AlumnoId = a.Id
                        ORDER BY c.Id DESC";

                    using (var cmd = new SqlCommand(query, conn))
                    using (var adapter = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        dgvColegiaturas.DataSource = dt;
                    }
                }
            }
            catch (Exception ex)
            {
                // Esto te dirá exactamente si hay un error de conexión o de columnas faltantes
                MessageBox.Show("Error detallado al cargar colegiaturas: " + ex.Message, "Error de Base de Datos", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnGuardar_Click(object sender, EventArgs e)
        {
            // Validaciones básicas de campos obligatorios
            if (string.IsNullOrWhiteSpace(txtMatricula.Text) || string.IsNullOrWhiteSpace(txtConcepto.Text) ||
                cmbMes.SelectedIndex == -1 || string.IsNullOrWhiteSpace(txtMonto.Text) || string.IsNullOrWhiteSpace(txtAnio.Text))
            {
                MostrarMensaje("Completa todos los campos obligatorios (Matrícula, Mes, Año y Monto).", true);
                return;
            }

            // Validar Monto
            if (!decimal.TryParse(txtMonto.Text.Trim(), out decimal monto) || monto <= 0)
            {
                MostrarMensaje("El monto debe ser un número válido mayor a 0.", true);
                return;
            }

            // Validar Año
            if (!int.TryParse(txtAnio.Text.Trim(), out int anio) || anio < 2000 || anio > 2100)
            {
                MostrarMensaje("Ingresa un año válido (ej. 2026).", true);
                return;
            }

            try
            {
                string connString = ConfigurationManager.ConnectionStrings["ArquimedesDB"].ConnectionString;
                using (var conn = new SqlConnection(connString))
                {
                    conn.Open();

                    string matriculaBuscada = txtMatricula.Text.Trim();
                    int alumnoId = 0;

                    // 1. Buscar si la matrícula existe en la base de datos
                    string queryId = "SELECT Id FROM Alumnos WHERE Matricula = @Matricula";
                    using (var cmdId = new SqlCommand(queryId, conn))
                    {
                        cmdId.Parameters.AddWithValue("@Matricula", matriculaBuscada);
                        object resultado = cmdId.ExecuteScalar();

                        if (resultado == null)
                        {
                            MostrarMensaje("No se encontró ningún alumno registrado con esa matrícula.", true);
                            return;
                        }

                        alumnoId = Convert.ToInt32(resultado);
                    }

                    // 2. Insertar la colegiatura con los datos validados
                    string queryInsert = @"
                        INSERT INTO Colegiaturas 
                            (AlumnoId, Concepto, Mes, Anio, Monto, FechaVencimiento, Estatus, MetodoPago, Observaciones, FechaPago)
                        VALUES 
                            (@AlumnoId, @Concepto, @Mes, @Anio, @Monto, @FechaVencimiento, @Estatus, @MetodoPago, @Observaciones, @FechaPago)";

                    using (var cmd = new SqlCommand(queryInsert, conn))
                    {
                        string estatusReal = cmbEstatus.SelectedItem != null ? cmbEstatus.SelectedItem.ToString() : "Pendiente";
                        object fechaPago = (estatusReal == "Pagado") ? (object)DateTime.Now : DBNull.Value;

                        cmd.Parameters.AddWithValue("@AlumnoId", alumnoId);
                        cmd.Parameters.AddWithValue("@Concepto", txtConcepto.Text.Trim());
                        cmd.Parameters.AddWithValue("@Mes", cmbMes.SelectedItem.ToString());
                        cmd.Parameters.AddWithValue("@Anio", anio);
                        cmd.Parameters.AddWithValue("@Monto", monto);
                        cmd.Parameters.AddWithValue("@FechaVencimiento", dtpVencimiento.Value.Date);
                        cmd.Parameters.AddWithValue("@Estatus", estatusReal);
                        cmd.Parameters.AddWithValue("@MetodoPago", cmbMetodoPago.SelectedItem != null ? (object)cmbMetodoPago.SelectedItem.ToString() : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Observaciones", string.IsNullOrWhiteSpace(txtObservaciones.Text) ? (object)DBNull.Value : txtObservaciones.Text.Trim());
                        cmd.Parameters.AddWithValue("@FechaPago", fechaPago);

                        cmd.ExecuteNonQuery();
                    }
                }

                MostrarMensaje("¡Colegiatura registrada correctamente!", false);
                LimpiarCampos();
                CargarColegiaturas();
            }
            catch (Exception ex)
            {
                MostrarMensaje("Error al guardar: " + ex.Message, true);
            }
        }

        private void btnMarcarPagado_Click(object sender, EventArgs e)
        {
            if (dgvColegiaturas.CurrentRow == null)
            {
                MostrarMensaje("Selecciona un registro de la lista.", true);
                return;
            }

            int id = Convert.ToInt32(dgvColegiaturas.CurrentRow.Cells["Id"].Value);

            try
            {
                string connString = ConfigurationManager.ConnectionStrings["ArquimedesDB"].ConnectionString;
                using (var conn = new SqlConnection(connString))
                {
                    conn.Open();
                    string query = "UPDATE Colegiaturas SET Estatus = 'Pagado', FechaPago = @FechaPago WHERE Id = @Id";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@FechaPago", DateTime.Now);
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.ExecuteNonQuery();
                    }
                }

                MostrarMensaje("Marcado como pagado exitosamente.", false);
                CargarColegiaturas();
            }
            catch (Exception ex)
            {
                MostrarMensaje("Error: " + ex.Message, true);
            }
        }

        private void btnEliminar_Click(object sender, EventArgs e)
        {
            if (dgvColegiaturas.CurrentRow == null)
            {
                MostrarMensaje("Selecciona un registro de la lista.", true);
                return;
            }

            var confirmacion = MessageBox.Show("¿Seguro que deseas eliminar este registro?",
                "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirmacion != DialogResult.Yes) return;

            int id = Convert.ToInt32(dgvColegiaturas.CurrentRow.Cells["Id"].Value);

            try
            {
                string connString = ConfigurationManager.ConnectionStrings["ArquimedesDB"].ConnectionString;
                using (var conn = new SqlConnection(connString))
                {
                    conn.Open();
                    string query = "DELETE FROM Colegiaturas WHERE Id = @Id";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.ExecuteNonQuery();
                    }
                }

                MostrarMensaje("Registro eliminado correctamente.", false);
                CargarColegiaturas();
            }
            catch (Exception ex)
            {
                MostrarMensaje("Error: " + ex.Message, true);
            }
        }

        private void LimpiarCampos()
        {
            txtMatricula.Clear();
            txtConcepto.Clear();
            txtMonto.Clear();
            txtAnio.Text = DateTime.Now.Year.ToString();
            txtObservaciones.Clear();
            if (cmbMes.Items.Count > 0) cmbMes.SelectedIndex = 0;
            if (cmbEstatus.Items.Count > 0) cmbEstatus.SelectedIndex = 0;
            cmbMetodoPago.SelectedIndex = -1;
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            lblMensaje.Text = mensaje;
            lblMensaje.ForeColor = esError ? Color.Red : Color.Green;
            lblMensaje.Visible = true;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Form1 form1 = new Form1();
            form1.Show();
            this.Hide();
        }

        private void btnGenerarPdf_Click(object sender, EventArgs e)
        {
            GeneradorPDF.GenerarComprobanteColegiatura(
                txtMatricula.Text.Trim(),
                "Alumno Registrado",
                txtConcepto.Text.Trim(),
                cmbMes.Text,
                txtAnio.Text.Trim(),
                txtMonto.Text.Trim(),
                cmbEstatus.Text
            );
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFileDialog saveDialog = new SaveFileDialog();
                saveDialog.Filter = "Archivo PDF (*.pdf)|*.pdf";
                saveDialog.FileName = "ReporteColegiaturas.pdf";

                if (saveDialog.ShowDialog() != DialogResult.OK)
                    return;

                string rutaArchivo = saveDialog.FileName;

                using (FileStream fs = new FileStream(rutaArchivo, FileMode.Create))
                {
                    // Se usa PageSize horizontal (Landscape)
                    Document doc = new Document(PageSize.A4.Rotate(), 20, 20, 25, 25);
                    PdfWriter.GetInstance(doc, fs);
                    doc.Open();

                    iTextSharp.text.Font fontTitulo = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                    Paragraph titulo = new Paragraph("Instituto Arquímedes - Reporte de Colegiaturas", fontTitulo);
                    titulo.Alignment = Element.ALIGN_CENTER;
                    titulo.SpacingAfter = 15;
                    doc.Add(titulo);

                    // Configurar tabla para el reporte de colegiaturas enlazado a la vista actual
                    PdfPTable tabla = new PdfPTable(8);
                    tabla.WidthPercentage = 100;
                    tabla.SetWidths(new float[] { 0.6f, 2.5f, 1.5f, 1f, 1f, 1f, 1.2f, 1.2f });

                    iTextSharp.text.Font fontEncabezado = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9);
                    string[] encabezados = { "Id", "Alumno", "Concepto", "Mes", "Año", "Monto", "Vencimiento", "Estatus" };

                    foreach (string encabezado in encabezados)
                    {
                        PdfPCell celda = new PdfPCell(new Phrase(encabezado, fontEncabezado));
                        celda.BackgroundColor = new BaseColor(220, 220, 220);
                        celda.HorizontalAlignment = Element.ALIGN_CENTER;
                        celda.Padding = 5;
                        tabla.AddCell(celda);
                    }

                    iTextSharp.text.Font fontContenido = FontFactory.GetFont(FontFactory.HELVETICA, 8);

                    foreach (DataGridViewRow fila in dgvColegiaturas.Rows)
                    {
                        if (fila.IsNewRow) continue;

                        tabla.AddCell(new PdfPCell(new Phrase(fila.Cells["Id"].Value?.ToString() ?? "", fontContenido)));
                        tabla.AddCell(new PdfPCell(new Phrase(fila.Cells["Alumno"].Value?.ToString() ?? "", fontContenido)));
                        tabla.AddCell(new PdfPCell(new Phrase(fila.Cells["Concepto"].Value?.ToString() ?? "", fontContenido)));
                        tabla.AddCell(new PdfPCell(new Phrase(fila.Cells["Mes"].Value?.ToString() ?? "", fontContenido)));
                        tabla.AddCell(new PdfPCell(new Phrase(fila.Cells["Anio"].Value?.ToString() ?? "", fontContenido)));
                        tabla.AddCell(new PdfPCell(new Phrase(fila.Cells["Monto"].Value?.ToString() ?? "", fontContenido)));

                        // Formatear la fecha de vencimiento con seguridad
                        string fechaVenc = "";
                        if (fila.Cells["FechaVencimiento"].Value != null && fila.Cells["FechaVencimiento"].Value != DBNull.Value)
                        {
                            fechaVenc = Convert.ToDateTime(fila.Cells["FechaVencimiento"].Value).ToString("dd/MM/yyyy");
                        }
                        tabla.AddCell(new PdfPCell(new Phrase(fechaVenc, fontContenido)));

                        tabla.AddCell(new PdfPCell(new Phrase(fila.Cells["Estatus"].Value?.ToString() ?? "", fontContenido)));
                    }

                    doc.Add(tabla);
                    doc.Close();
                }

                MessageBox.Show("PDF generado correctamente.", "Éxito",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Abrir el PDF generado de forma segura
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(rutaArchivo) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar el PDF:\n\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            Form4 form4 = new Form4();
            form4.Show();
            this.Hide();
        }
    }
}