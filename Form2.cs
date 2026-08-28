using QRCoder;
using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace Arquimedes
{
    public partial class Form2 : Form
    {
        // Aquí se guardará la imagen en formato byte[]
        private byte[] fotoSeleccionada = null;

        public Form2()
        {
            InitializeComponent();
        }

        // =========================================================
        // FORM LOAD
        // =========================================================
        private void Form2_Load(object sender, EventArgs e)
        {
            ConfigurarControles();
        }

        // =========================================================
        // CONFIGURAR CONTROLES
        // =========================================================
        private void ConfigurarControles()
        {
            numAnioIngreso.Minimum = 2020;
            numAnioIngreso.Maximum = 2100;
            numAnioIngreso.Value = DateTime.Now.Year;

            numLista.Minimum = 1;
            numLista.Maximum = 99;
            numLista.Value = 1;
        }

        // =========================================================
        // CARGAR FOTO
        // =========================================================
        private void CargarFoto()
        {
            using (OpenFileDialog dialogo = new OpenFileDialog())
            {
                dialogo.Filter =
                    "Imágenes (*.jpg;*.jpeg;*.png;*.bmp;*.gif)|*.jpg;*.jpeg;*.png;*.bmp;*.gif";

                dialogo.Title = "Selecciona la foto del alumno";

                if (dialogo.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Leer directamente el archivo como bytes
                        fotoSeleccionada = File.ReadAllBytes(dialogo.FileName);

                        // Crear una copia independiente de la imagen
                        using (MemoryStream ms =
                            new MemoryStream(fotoSeleccionada))
                        {
                            using (Image imagenTemporal =
                                Image.FromStream(ms))
                            {
                                if (picFoto.Image != null)
                                {
                                    picFoto.Image.Dispose();
                                    picFoto.Image = null;
                                }

                                picFoto.Image =
                                    new Bitmap(imagenTemporal);
                            }
                        }

                        picFoto.SizeMode =
                            PictureBoxSizeMode.Zoom;

                        MessageBox.Show(
                            "Foto cargada correctamente.",
                            "Foto",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                    }
                    catch (Exception ex)
                    {
                        fotoSeleccionada = null;

                        MessageBox.Show(
                            "No se pudo cargar la imagen:\n\n" +
                            ex.Message,
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                    }
                }
            }
        }

        // =========================================================
        // BOTÓN CARGAR FOTO
        // =========================================================
        private void btnCargarFoto_Click(object sender, EventArgs e)
        {
            CargarFoto();
        }

        // =========================================================
        // ESTE EVENTO TAMBIÉN LLAMA AL MISMO MÉTODO
        // POR SI EL DISEÑADOR TIENE CONECTADO ESTE EVENTO
        // =========================================================
        private void btnCargarFoto_Click_1(object sender, EventArgs e)
        {
            CargarFoto();
        }

        // =========================================================
        // GENERAR MATRÍCULA
        // =========================================================
        private bool TryGenerarMatricula(
            out string matricula,
            out string errorMensaje)
        {
            matricula = null;
            errorMensaje = null;

            string gradoTexto =
                txtGrado.Text.Trim();

            string grupoTexto =
                txtGrupo.Text.Trim();

            // Validar grado
            if (!int.TryParse(
                    gradoTexto,
                    out int grado) ||
                grado < 1 ||
                grado > 6)
            {
                errorMensaje =
                    "El Grado debe ser un número del 1 al 6.";

                return false;
            }

            // Validar grupo
            if (string.IsNullOrEmpty(grupoTexto) ||
                (grupoTexto.ToUpper() != "A" &&
                 grupoTexto.ToUpper() != "B"))
            {
                errorMensaje =
                    "El Grupo debe ser 'A' o 'B'.";

                return false;
            }

            char grupo =
                grupoTexto.ToUpper()[0];

            int anio =
                (int)numAnioIngreso.Value;

            int numeroLista =
                (int)numLista.Value;

            string grado1Digito =
                grado.ToString();

            string grupoDigito =
                (grupo == 'A') ? "1" : "2";

            string anio2Digitos =
                (anio % 100).ToString("D2");

            string listaDosDigitos =
                numeroLista.ToString("D2");

            // Ejemplo:
            // Grado 1
            // Grupo A
            // Año 2026
            // Lista 01
            //
            // Resultado: 112601

            matricula =
                grado1Digito +
                grupoDigito +
                anio2Digitos +
                listaDosDigitos;

            return true;
        }

        // =========================================================
        // GUARDAR ALUMNO
        // =========================================================
        private void button1_Click(object sender, EventArgs e)
        {
            // =====================================================
            // VALIDAR NOMBRE Y APELLIDO
            // =====================================================

            if (string.IsNullOrWhiteSpace(txtNombre.Text) ||
                string.IsNullOrWhiteSpace(txtApellido.Text))
            {
                MessageBox.Show(
                    "Por favor completa al menos Nombre y Apellido.",
                    "Datos incompletos",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }

            // =====================================================
            // VALIDAR MATRÍCULA
            // =====================================================

            if (!TryGenerarMatricula(
                    out string matricula,
                    out string errorMatricula))
            {
                MessageBox.Show(
                    errorMatricula,
                    "Datos incompletos",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }

            // =====================================================
            // VALIDAR FOTO
            // =====================================================

            if (fotoSeleccionada == null ||
                fotoSeleccionada.Length == 0)
            {
                DialogResult respuesta =
                    MessageBox.Show(
                        "No has seleccionado una foto para el alumno.\n\n" +
                        "¿Deseas continuar sin foto?",
                        "Foto no seleccionada",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question
                    );

                if (respuesta != DialogResult.Yes)
                {
                    return;
                }
            }

            int anio =
                (int)numAnioIngreso.Value;

            int numeroLista =
                (int)numLista.Value;


            // =====================================================
            // CONEXIÓN
            // =====================================================

            string connectionString =
                @"Data Source=(localdb)\MSSQLLocalDB;" +
                @"Initial Catalog=ArquimedesDB;" +
                @"Integrated Security=True;";


            try
            {
                using (SqlConnection conn =
                    new SqlConnection(connectionString))
                {
                    conn.Open();


                    // =================================================
                    // INSERTAR ALUMNO
                    // =================================================

                    string sqlInsert = @"
                        INSERT INTO Alumnos
                        (
                            Nombre,
                            Apellido,
                            Grado,
                            Grupo,
                            AnioIngreso,
                            NumLista,
                            Matricula,
                            Foto,
                            QR
                        )
                        OUTPUT INSERTED.Id
                        VALUES
                        (
                            @Nombre,
                            @Apellido,
                            @Grado,
                            @Grupo,
                            @AnioIngreso,
                            @NumLista,
                            @Matricula,
                            @Foto,
                            @QRVacio
                        )";


                    int nuevoId;


                    using (SqlCommand cmd =
                        new SqlCommand(sqlInsert, conn))
                    {
                        cmd.Parameters.Add(
                            "@Nombre",
                            System.Data.SqlDbType.NVarChar
                        ).Value =
                            txtNombre.Text.Trim();


                        cmd.Parameters.Add(
                            "@Apellido",
                            System.Data.SqlDbType.NVarChar
                        ).Value =
                            txtApellido.Text.Trim();


                        cmd.Parameters.Add(
                            "@Grado",
                            System.Data.SqlDbType.NVarChar
                        ).Value =
                            txtGrado.Text.Trim();


                        cmd.Parameters.Add(
                            "@Grupo",
                            System.Data.SqlDbType.NVarChar
                        ).Value =
                            txtGrupo.Text.Trim();


                        cmd.Parameters.Add(
                            "@AnioIngreso",
                            System.Data.SqlDbType.Int
                        ).Value =
                            anio;


                        cmd.Parameters.Add(
                            "@NumLista",
                            System.Data.SqlDbType.Int
                        ).Value =
                            numeroLista;


                        cmd.Parameters.Add(
                            "@Matricula",
                            System.Data.SqlDbType.NVarChar
                        ).Value =
                            matricula;


                        // =============================================
                        // FOTO
                        // =============================================

                        SqlParameter parametroFoto =
                            cmd.Parameters.Add(
                                "@Foto",
                                System.Data.SqlDbType.VarBinary,
                                -1
                            );


                        if (fotoSeleccionada != null &&
                            fotoSeleccionada.Length > 0)
                        {
                            parametroFoto.Value =
                                fotoSeleccionada;
                        }
                        else
                        {
                            parametroFoto.Value =
                                DBNull.Value;
                        }


                        // =============================================
                        // QR VACÍO
                        // =============================================

                        SqlParameter parametroQR =
                            cmd.Parameters.Add(
                                "@QRVacio",
                                System.Data.SqlDbType.VarBinary,
                                -1
                            );

                        parametroQR.Value =
                            new byte[0];


                        nuevoId =
                            Convert.ToInt32(
                                cmd.ExecuteScalar()
                            );
                    }


                    // =================================================
                    // GENERAR QR
                    // =================================================

                    string contenidoQR =
                        "ALUM-" + nuevoId;


                    QRCodeGenerator qrGenerator =
                        new QRCodeGenerator();


                    QRCodeData qrCodeData =
                        qrGenerator.CreateQrCode(
                            contenidoQR,
                            QRCodeGenerator.ECCLevel.Q
                        );


                    QRCode qrCode =
                        new QRCode(qrCodeData);


                    Bitmap qrImage =
                        qrCode.GetGraphic(10);


                    // =================================================
                    // CONVERTIR QR A BYTES
                    // =================================================

                    byte[] qrBytes;


                    using (MemoryStream ms =
                        new MemoryStream())
                    {
                        qrImage.Save(
                            ms,
                            ImageFormat.Png
                        );

                        qrBytes =
                            ms.ToArray();
                    }


                    // =================================================
                    // GUARDAR QR EN SQL SERVER
                    // =================================================

                    string sqlUpdate =
                        "UPDATE Alumnos " +
                        "SET QR = @QR " +
                        "WHERE Id = @Id";


                    using (SqlCommand cmd =
                        new SqlCommand(
                            sqlUpdate,
                            conn))
                    {
                        cmd.Parameters.Add(
                            "@QR",
                            System.Data.SqlDbType.VarBinary,
                            -1
                        ).Value =
                            qrBytes;


                        cmd.Parameters.Add(
                            "@Id",
                            System.Data.SqlDbType.Int
                        ).Value =
                            nuevoId;


                        cmd.ExecuteNonQuery();
                    }


                    // =================================================
                    // MOSTRAR QR
                    // =================================================

                    if (pictureBoxQR.Image != null)
                    {
                        pictureBoxQR.Image.Dispose();
                        pictureBoxQR.Image = null;
                    }


                    pictureBoxQR.Image =
                        new Bitmap(qrImage);


                    pictureBoxQR.SizeMode =
                        PictureBoxSizeMode.Zoom;


                    // =================================================
                    // GUARDAR QR COMO ARCHIVO
                    // =================================================

                    string carpetaQR =
                        Path.Combine(
                            Application.StartupPath,
                            "QRs"
                        );


                    Directory.CreateDirectory(
                        carpetaQR
                    );


                    string rutaQR =
                        Path.Combine(
                            carpetaQR,
                            $"alumno_{nuevoId}.png"
                        );


                    qrImage.Save(
                        rutaQR,
                        ImageFormat.Png
                    );


                    // =================================================
                    // MOSTRAR MENSAJE DE ÉXITO
                    // =================================================

                    string nombreCompleto =
                        $"{txtNombre.Text.Trim()} " +
                        $"{txtApellido.Text.Trim()}";


                    FrmExito frmExito =
                        new FrmExito(
                            nombreCompleto,
                            qrImage
                        );


                    frmExito.ShowDialog();


                    // =================================================
                    // LIMPIAR CAMPOS
                    // =================================================

                    LimpiarCampos();


                    // Liberar QR
                    qrImage.Dispose();
                    qrCode.Dispose();
                    qrCodeData.Dispose();
                }
            }
            catch (SqlException ex)
                when (ex.Number == 2627 ||
                      ex.Number == 2601)
            {
                MessageBox.Show(
                    "Ya existe un alumno con esa matrícula " +
                    "(grado/grupo/año/lista repetidos).",
                    "Matrícula duplicada",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error al guardar:\n\n" +
                    ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        // =========================================================
        // LIMPIAR CAMPOS
        // =========================================================
        private void LimpiarCampos()
        {
            txtNombre.Clear();
            txtApellido.Clear();
            txtGrado.Clear();
            txtGrupo.Clear();

            numAnioIngreso.Value =
                DateTime.Now.Year;

            numLista.Value = 1;


            if (picFoto.Image != null)
            {
                picFoto.Image.Dispose();
                picFoto.Image = null;
            }


            fotoSeleccionada = null;


            if (pictureBoxQR.Image != null)
            {
                pictureBoxQR.Image.Dispose();
                pictureBoxQR.Image = null;
            }
        }

        // =========================================================
        // REGRESAR
        // =========================================================
        private void button3_Click(object sender, EventArgs e)
        {
            Form1 form1 = new Form1();

            form1.Show();

            this.Hide();
        }
    }
}