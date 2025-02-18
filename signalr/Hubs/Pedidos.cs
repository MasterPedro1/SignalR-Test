using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;


namespace signalr.Hubs
{
    public class Pedidos : Hub
    {
        private static List<Pedido> _pedidos = new List<Pedido>();

        public async Task EnviarPedido(Pedido nuevoPedido)
        {
            nuevoPedido.Estatus = "En revisión";  // Cambia a "En revisión" al enviarlo
            _pedidos.Add(nuevoPedido);
            await Clients.All.SendAsync("ActualizarPedidos", _pedidos);
        }

        public async Task EliminarPedido(string usuario, string idPedido)
        {
            var pedido = _pedidos.FirstOrDefault(p => p.Id == idPedido && p.Usuario == usuario);
            if (pedido != null)
            {
                _pedidos.Remove(pedido);
                await Clients.All.SendAsync("ActualizarPedidos", _pedidos);
            }
        }

        public async Task ObtenerPedidos()
        {
            await Clients.Caller.SendAsync("ActualizarPedidos", _pedidos);
        }
    }

    public class Pedido
    {
        public string Id { get; set; } = System.Guid.NewGuid().ToString();
        public string PedidoNombre { get; set; }
        public string Cantidad { get; set; }
        public string Fecha { get; set; }
        public string Usuario { get; set; }
        public string Estatus { get; set; }
    }
}