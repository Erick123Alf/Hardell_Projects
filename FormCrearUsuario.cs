using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace Arquimedes
{
    public partial class FormCrearUsuario : Form
    {
        public FormCrearUsuario()
        {
            InitializeComponent();
            lblMensaje.Visible = false;
        }

        private void btnGuardar_Click(object sender, EventArgs e)
        {
            string usuario = txtNuevoUsuario.Text.Trim();
            string password = txtNuevaPassword.Text;
            string confirmar = txtConfirmarPassword.Text;

            if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(password))
            {
                MostrarMensaje("Completa usuario y contraseña.", true);
                return;
            }

            if (password.Length < 6)
            {
                MostrarMensaje("La contraseña debe tener al menos 6 caracteres.", true);
                return;
            }

            if (password != confirmar)
            {
                MostrarMensaje("Las contraseñas no coinciden.", true);
                return;
            }

            try
            {
                if (ExisteUsuario(usuario))
                {
                    MostrarMensaje("Ese usuario ya existe.", true);
                    return;
                }

                CrearUsuario(usuario, password);
                MostrarMensaje("Usuario creado correctamente.", false);

                txtNuevoUsuario.Clear();
                txtNuevaPassword.Clear();
                txtConfirmarPassword.Clear();
            }
            catch (SqlException)
            {
                MostrarMensaje("No se pudo conectar con la base de datos.", true);
            }
            catch (Exception ex)
            {
                MostrarMensaje("Error inesperado: " + ex.Message, true);
            }
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private bool ExisteUsuario(string usuario)
        {
            string connString = ConfigurationManager.ConnectionStrings["ArquimedesDB"].ConnectionString;

            using (var conn = new SqlConnection(connString))
            {
                conn.Open();
                string query = "SELECT COUNT(*) FROM Administradores WHERE LOWER(Usuario) = LOWER(@usuario)";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@usuario", usuario);
                    int count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }

        private void CrearUsuario(string usuario, string password)
        {
            string hash = CalcularHash(password);
            string connString = ConfigurationManager.ConnectionStrings["ArquimedesDB"].ConnectionString;

            using (var conn = new SqlConnection(connString))
            {
                conn.Open();
                string query = "INSERT INTO Administradores (Usuario, PasswordHash) VALUES (@usuario, @hash)";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@usuario", usuario);
                    cmd.Parameters.AddWithValue("@hash", hash);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            lblMensaje.Text = mensaje;
            lblMensaje.ForeColor = esError ? Color.Red : Color.Green;
            lblMensaje.Visible = true;
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