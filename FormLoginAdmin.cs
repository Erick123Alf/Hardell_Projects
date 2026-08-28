using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace Arquimedes
{
    public partial class FormLoginAdmin : Form
    {
        public FormLoginAdmin()
        {
            InitializeComponent();
            lblError.Visible = false;
        }

        private void btnIngresar_Click(object sender, EventArgs e)
        {
            string usuario = txtUsuario.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(password))
            {
                MostrarError("Ingresa usuario y contraseña.");
                return;
            }

            try
            {
                if (ValidarCredenciales(usuario, password))
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                    MessageBox.Show("Bienvenido, " + usuario + "!", "Acceso concedido", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MostrarError("Usuario o contraseña incorrectos.");
                    txtPassword.Clear();
                    txtPassword.Focus();
                }
            }
            catch (SqlException)
            {
                MostrarError("No se pudo conectar con la base de datos. Verifica la conexión de red.");
            }
            catch (Exception ex)
            {
                MostrarError("Ocurrió un error inesperado: " + ex.Message);
            }

            Form4 form4 = new Form4();
            form4.Show();
            this.Hide();
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void txtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                btnIngresar_Click(sender, e);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (var frmCrear = new FormCrearUsuario())
            {
                frmCrear.ShowDialog();
            }
        }

        private void MostrarError(string mensaje)
        {
            lblError.Text = mensaje;
            lblError.Visible = true;
        }

        private bool ValidarCredenciales(string usuario, string password)
        {
            string hashIngresado = CalcularHash(password);
            string connString = ConfigurationManager.ConnectionStrings["ArquimedesDB"].ConnectionString;

            using (var conn = new SqlConnection(connString))
            {
                conn.Open();
                string query = "SELECT PasswordHash FROM Administradores WHERE LOWER(Usuario) = LOWER(@usuario)";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@usuario", usuario);
                    var resultado = cmd.ExecuteScalar();

                    if (resultado == null)
                        return false;

                    string hashGuardado = resultado.ToString();
                    return string.Equals(hashGuardado, hashIngresado, StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        private string CalcularHash(string texto)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(texto));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in bytes)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}