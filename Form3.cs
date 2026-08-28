
using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using iTextSharp.text;
using iTextSharp.text.pdf;
using PdfImage = iTextSharp.text.Image;
using PdfFont = iTextSharp.text.Font;
using PdfParagraph = iTextSharp.text.Paragraph;

namespace Arquimedes
{
    public partial class Form3 : Form
    {
        public Form3()
        {
            InitializeComponent();
        }

        // ============================================================
        // CARGAR ALUMNOS
        // ============================================================
        private void CargarAlumnos()
        {
            string connectionString =
                @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=ArquimedesDB;Integrated Security=True;";

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string sql = @"
                        SELECT 
                            Id,
                            Matricula,
                            Nombre,
                            Apellido,
                            Grado,
                            Grupo,
                            AnioIngreso,
                            NumLista,
                            Foto,
                            QR
                        FROM Alumnos";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        DataTable tabla = new DataTable();

                        tabla.Columns.Add("Id", typeof(int));
                        tabla.Columns.Add("Matricula", typeof(string));
                        tabla.Columns.Add("Nombre", typeof(string));
                        tabla.Columns.Add("Apellido", typeof(string));
                        tabla.Columns.Add("Grado", typeof(string));
                        tabla.Columns.Add("Grupo", typeof(string));
                        tabla.Columns.Add("AnioIngreso", typeof(int));
                        tabla.Columns.Add("NumLista", typeof(int));

                        // IMPORTANTE:
                        // Estas dos columnas deben ser de tipo Image
                        tabla.Columns.Add("Foto", typeof(System.Drawing.Image));
                        tabla.Columns.Add("QR", typeof(System.Drawing.Image));

                        while (reader.Read())
                        {
                            DataRow fila = tabla.NewRow();

                            // ====================================================
                            // DATOS DEL ALUMNO
                            // ====================================================

                            fila["Id"] = reader["Id"];

                            fila["Matricula"] =
                                reader["Matricula"] != DBNull.Value
                                    ? reader["Matricula"].ToString()
                                    : "";

                            fila["Nombre"] =
                                reader["Nombre"] != DBNull.Value
                                    ? reader["Nombre"].ToString()
                                    : "";

                            fila["Apellido"] =
                                reader["Apellido"] != DBNull.Value
                                    ? reader["Apellido"].ToString()
                                    : "";

                            fila["Grado"] =
                                reader["Grado"] != DBNull.Value
                                    ? reader["Grado"].ToString()
                                    : "";

                            fila["Grupo"] =
                                reader["Grupo"] != DBNull.Value
                                    ? reader["Grupo"].ToString()
                                    : "";

                            fila["AnioIngreso"] =
                                reader["AnioIngreso"] != DBNull.Value
                                    ? Convert.ToInt32(reader["AnioIngreso"])
                                    : 0;

                            fila["NumLista"] =
                                reader["NumLista"] != DBNull.Value
                                    ? Convert.ToInt32(reader["NumLista"])
                                    : 0;


                            // ====================================================
                            // PROCESAR FOTO DEL ALUMNO
                            // ====================================================

                            if (reader["Foto"] != DBNull.Value)
                            {
                                try
                                {
                                    byte[] fotoBytes = (byte[])reader["Foto"];

                                    if (fotoBytes != null && fotoBytes.Length > 0)
                                    {
                                        using (MemoryStream ms =
                                            new MemoryStream(fotoBytes))
                                        {
                                            using (System.Drawing.Image imagenTemporal =
                                                System.Drawing.Image.FromStream(ms))
                                            {
                                                // Crear una copia independiente
                                                // del MemoryStream
                                                fila["Foto"] =
                                                    new Bitmap(imagenTemporal);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        fila["Foto"] = DBNull.Value;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show(
                                        $"Error al cargar la foto del alumno ID {reader["Id"]}:\n\n{ex.Message}",
                                        "Error de imagen",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Warning
                                    );

                                    fila["Foto"] = DBNull.Value;
                                }
                            }
                            else
                            {
                                fila["Foto"] = DBNull.Value;
                            }


                            // ====================================================
                            // PROCESAR QR
                            // ====================================================

                            if (reader["QR"] != DBNull.Value)
                            {
                                try
                                {
                                    byte[] qrBytes = (byte[])reader["QR"];

                                    if (qrBytes != null && qrBytes.Length > 0)
                                    {
                                        using (MemoryStream ms =
                                            new MemoryStream(qrBytes))
                                        {
                                            using (System.Drawing.Image imagenTemporal =
                                                System.Drawing.Image.FromStream(ms))
                                            {
                                                // Crear copia independiente
                                                fila["QR"] =
                                                    new Bitmap(imagenTemporal);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        fila["QR"] = DBNull.Value;
                                    }
                                }
                                catch
                                {
                                    fila["QR"] = DBNull.Value;
                                }
                            }
                            else
                            {
                                fila["QR"] = DBNull.Value;
                            }


                            // Agregar fila
                            tabla.Rows.Add(fila);
                        }


                        // ====================================================
                        // MOSTRAR DATOS EN DATAGRIDVIEW
                        // ====================================================

                        dgvAlumnos.DataSource = tabla;


                        // ====================================================
                        // CONFIGURAR COLUMNA FOTO
                        // ====================================================

                        if (dgvAlumnos.Columns["Foto"]
                            is DataGridViewImageColumn colFoto)
                        {
                            colFoto.ImageLayout =
                                DataGridViewImageCellLayout.Zoom;

                            colFoto.DefaultCellStyle.NullValue = null;
                        }


                        // ====================================================
                        // CONFIGURAR COLUMNA QR
                        // ====================================================

                        if (dgvAlumnos.Columns["QR"]
                            is DataGridViewImageColumn colQR)
                        {
                            colQR.ImageLayout =
                                DataGridViewImageCellLayout.Zoom;

                            colQR.DefaultCellStyle.NullValue = null;
                        }


                        // ====================================================
                        // ALTURA DE LAS FILAS
                        // ====================================================

                        dgvAlumnos.AutoSizeRowsMode =
                            DataGridViewAutoSizeRowsMode.None;

                        foreach (DataGridViewRow fila in dgvAlumnos.Rows)
                        {
                            fila.Height = 90;
                        }


                        // ====================================================
                        // ANCHOS DE COLUMNAS
                        // ====================================================

                        if (dgvAlumnos.Columns["Id"] != null)
                            dgvAlumnos.Columns["Id"].Width = 40;

                        if (dgvAlumnos.Columns["Matricula"] != null)
                            dgvAlumnos.Columns["Matricula"].Width = 80;

                        if (dgvAlumnos.Columns["Nombre"] != null)
                            dgvAlumnos.Columns["Nombre"].Width = 100;

                        if (dgvAlumnos.Columns["Apellido"] != null)
                            dgvAlumnos.Columns["Apellido"].Width = 100;

                        if (dgvAlumnos.Columns["Grado"] != null)
                            dgvAlumnos.Columns["Grado"].Width = 50;

                        if (dgvAlumnos.Columns["Grupo"] != null)
                            dgvAlumnos.Columns["Grupo"].Width = 50;

                        if (dgvAlumnos.Columns["AnioIngreso"] != null)
                            dgvAlumnos.Columns["AnioIngreso"].Width = 70;

                        if (dgvAlumnos.Columns["NumLista"] != null)
                            dgvAlumnos.Columns["NumLista"].Width = 60;

                        if (dgvAlumnos.Columns["Foto"] != null)
                            dgvAlumnos.Columns["Foto"].Width = 90;

                        if (dgvAlumnos.Columns["QR"] != null)
                            dgvAlumnos.Columns["QR"].Width = 90;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "❌ Error al cargar los alumnos:\n\n" +
                    ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }


        // ============================================================
        // FORM LOAD
        // ============================================================
        private void Form3_Load(object sender, EventArgs e)
        {
            CargarAlumnos();
        }


        // ============================================================
        // CLICK EN DATAGRIDVIEW
        // ============================================================
        private void dgvAlumnos_CellContentClick(
            object sender,
            DataGridViewCellEventArgs e)
        {

        }


        // ============================================================
        // ELIMINAR ALUMNO
        // ============================================================
        private void button1_Click(object sender, EventArgs e)
        {
            if (dgvAlumnos.CurrentRow == null ||
                dgvAlumnos.SelectedRows.Count == 0)
            {
                MessageBox.Show(
                    "Selecciona un alumno de la lista para eliminar.",
                    "Aviso",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }


            int idSeleccionado =
                Convert.ToInt32(
                    dgvAlumnos.CurrentRow.Cells["Id"].Value
                );


            string nombreCompleto =
                $"{dgvAlumnos.CurrentRow.Cells["Nombre"].Value} " +
                $"{dgvAlumnos.CurrentRow.Cells["Apellido"].Value}";


            DialogResult confirmacion = MessageBox.Show(
                $"¿Seguro que deseas eliminar a {nombreCompleto}?",
                "Confirmar eliminación",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );


            if (confirmacion != DialogResult.Yes)
                return;


            string connectionString =
                @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=ArquimedesDB;Integrated Security=True;";


            try
            {
                using (SqlConnection conn =
                    new SqlConnection(connectionString))
                {
                    conn.Open();


                    string sql =
                        "DELETE FROM Alumnos WHERE Id = @Id";


                    using (SqlCommand cmd =
                        new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue(
                            "@Id",
                            idSeleccionado
                        );

                        cmd.ExecuteNonQuery();
                    }
                }


                MessageBox.Show(
                    $"{nombreCompleto} fue eliminado correctamente.",
                    "Éxito",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );


                CargarAlumnos();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error al eliminar:\n\n" +
                    ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }


        // ============================================================
        // REGRESAR AL FORM1
        // ============================================================
        private void button2_Click(object sender, EventArgs e)
        {
            Form1 form1 = new Form1();

            form1.Show();

            this.Hide();
        }


        // ============================================================
        // GENERAR PDF
        // ============================================================
        private void btnPdf_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFileDialog saveDialog =
                    new SaveFileDialog();

                saveDialog.Filter =
                    "Archivo PDF (*.pdf)|*.pdf";

                saveDialog.FileName =
                    "ListaAlumnos.pdf";


                if (saveDialog.ShowDialog() != DialogResult.OK)
                    return;


                string rutaArchivo =
                    saveDialog.FileName;


                using (FileStream fs =
                    new FileStream(
                        rutaArchivo,
                        FileMode.Create))
                {
                    Document doc =
                        new Document(
                            PageSize.A4.Rotate(),
                            20,
                            20,
                            25,
                            25
                        );


                    PdfWriter.GetInstance(doc, fs);

                    doc.Open();


                    // ====================================================
                    // TITULO
                    // ====================================================

                    PdfFont fontTitulo =
                        FontFactory.GetFont(
                            FontFactory.HELVETICA_BOLD,
                            16
                        );


                    PdfParagraph titulo =
                        new PdfParagraph(
                            "Instituto Arquímedes - Lista de Alumnos",
                            fontTitulo
                        );


                    titulo.Alignment =
                        Element.ALIGN_CENTER;

                    titulo.SpacingAfter = 15;

                    doc.Add(titulo);


                    // ====================================================
                    // TABLA PDF
                    // ====================================================

                    PdfPTable tabla =
                        new PdfPTable(10);


                    tabla.WidthPercentage = 100;


                    tabla.SetWidths(
                        new float[]
                        {
                            0.6f,
                            1.2f,
                            2f,
                            2f,
                            0.8f,
                            0.8f,
                            1f,
                            0.8f,
                            1.5f,
                            1.5f
                        }
                    );


                    PdfFont fontEncabezado =
                        FontFactory.GetFont(
                            FontFactory.HELVETICA_BOLD,
                            9
                        );


                    string[] encabezados =
                    {
                        "Id",
                        "Matrícula",
                        "Nombre",
                        "Apellido",
                        "Grado",
                        "Grupo",
                        "Ingreso",
                        "N° Lista",
                        "Foto",
                        "QR"
                    };


                    foreach (string encabezado in encabezados)
                    {
                        PdfPCell celda =
                            new PdfPCell(
                                new Phrase(
                                    encabezado,
                                    fontEncabezado
                                )
                            );


                        celda.BackgroundColor =
                            new BaseColor(220, 220, 220);

                        celda.HorizontalAlignment =
                            Element.ALIGN_CENTER;

                        celda.Padding = 5;


                        tabla.AddCell(celda);
                    }


                    // ====================================================
                    // RECORRER ALUMNOS
                    // ====================================================

                    foreach (DataGridViewRow fila
                        in dgvAlumnos.Rows)
                    {
                        if (fila.IsNewRow)
                            continue;


                        tabla.AddCell(
                            fila.Cells["Id"].Value?.ToString() ?? ""
                        );

                        tabla.AddCell(
                            fila.Cells["Matricula"].Value?.ToString() ?? ""
                        );

                        tabla.AddCell(
                            fila.Cells["Nombre"].Value?.ToString() ?? ""
                        );

                        tabla.AddCell(
                            fila.Cells["Apellido"].Value?.ToString() ?? ""
                        );

                        tabla.AddCell(
                            fila.Cells["Grado"].Value?.ToString() ?? ""
                        );

                        tabla.AddCell(
                            fila.Cells["Grupo"].Value?.ToString() ?? ""
                        );

                        tabla.AddCell(
                            fila.Cells["AnioIngreso"].Value?.ToString() ?? ""
                        );

                        tabla.AddCell(
                            fila.Cells["NumLista"].Value?.ToString() ?? ""
                        );


                        // ====================================================
                        // FOTO EN PDF
                        // ====================================================

                        var fotoValue =
                            fila.Cells["Foto"].Value;


                        if (fotoValue != null &&
                            fotoValue != DBNull.Value)
                        {
                            try
                            {
                                System.Drawing.Image imagenFoto =
                                    (System.Drawing.Image)fotoValue;


                                using (MemoryStream ms =
                                    new MemoryStream())
                                {
                                    imagenFoto.Save(
                                        ms,
                                        System.Drawing.Imaging.ImageFormat.Png
                                    );


                                    PdfImage pdfImgFoto =
                                        PdfImage.GetInstance(
                                            ms.ToArray()
                                        );


                                    pdfImgFoto.ScaleToFit(
                                        40f,
                                        40f
                                    );


                                    PdfPCell celdaFoto =
                                        new PdfPCell(
                                            pdfImgFoto,
                                            false
                                        );


                                    celdaFoto.HorizontalAlignment =
                                        Element.ALIGN_CENTER;

                                    celdaFoto.VerticalAlignment =
                                        Element.ALIGN_MIDDLE;

                                    celdaFoto.FixedHeight =
                                        45f;


                                    tabla.AddCell(
                                        celdaFoto
                                    );
                                }
                            }
                            catch
                            {
                                tabla.AddCell("");
                            }
                        }
                        else
                        {
                            tabla.AddCell("");
                        }


                        // ====================================================
                        // QR EN PDF
                        // ====================================================

                        var qrValue =
                            fila.Cells["QR"].Value;


                        if (qrValue != null &&
                            qrValue != DBNull.Value)
                        {
                            try
                            {
                                System.Drawing.Image imagenQR =
                                    (System.Drawing.Image)qrValue;


                                using (MemoryStream ms =
                                    new MemoryStream())
                                {
                                    imagenQR.Save(
                                        ms,
                                        System.Drawing.Imaging.ImageFormat.Png
                                    );


                                    PdfImage pdfImgQR =
                                        PdfImage.GetInstance(
                                            ms.ToArray()
                                        );


                                    pdfImgQR.ScaleToFit(
                                        40f,
                                        40f
                                    );


                                    PdfPCell celdaQR =
                                        new PdfPCell(
                                            pdfImgQR,
                                            false
                                        );


                                    celdaQR.HorizontalAlignment =
                                        Element.ALIGN_CENTER;

                                    celdaQR.VerticalAlignment =
                                        Element.ALIGN_MIDDLE;

                                    celdaQR.FixedHeight =
                                        45f;


                                    tabla.AddCell(
                                        celdaQR
                                    );
                                }
                            }
                            catch
                            {
                                tabla.AddCell("");
                            }
                        }
                        else
                        {
                            tabla.AddCell("");
                        }
                    }


                    // ====================================================
                    // AGREGAR TABLA AL PDF
                    // ====================================================

                    doc.Add(tabla);

                    doc.Close();
                }


                MessageBox.Show(
                    "PDF generado correctamente.",
                    "Éxito",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );


                // Abrir PDF automáticamente
                System.Diagnostics.Process.Start(
                    rutaArchivo
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error al generar el PDF:\n\n" +
                    ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
    }
}

