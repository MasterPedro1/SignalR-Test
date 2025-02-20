using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace signalr.Hubs
{
    public class Pedidos : Hub
    {
        private static List<Pedido> _pedidos = new List<Pedido>();
        private static Dictionary<string, Pedido> _pedidosTemporales = new Dictionary<string, Pedido>();

        public async Task EnviarPedido(Pedido nuevoPedido)
        {
            nuevoPedido.Estatus = "En revisión";

            // Definir el ID temporal basado en el usuario
            string idTemporal = $"temp-{nuevoPedido.Usuario}";

            // Eliminar la versión temporal del pedido enviado
            if (_pedidosTemporales.ContainsKey(idTemporal))
            {
                _pedidosTemporales.Remove(idTemporal);
                await Clients.All.SendAsync("EliminarFilaTemporal", idTemporal);
            }

            _pedidos.Add(nuevoPedido);
            await Clients.All.SendAsync("PedidoConfirmado", nuevoPedido);
        }

        public async Task EliminarPedido(string usuario, string idPedido)
        {
            var pedido = _pedidos.FirstOrDefault(p => p.Id == idPedido && p.Usuario == usuario);
            if (pedido != null)
            {
                _pedidos.Remove(pedido);
                await ActualizarPedidos();
            }
        }

        public async Task ObtenerPedidos()
        {
            await Clients.Caller.SendAsync("ActualizarPedidos", _pedidos.Concat(_pedidosTemporales.Values).ToList());
        }

        public async Task ActualizarFilaTemporal(Pedido tempPedido)
        {
            if (!string.IsNullOrEmpty(tempPedido.PedidoNombre) && !string.IsNullOrEmpty(tempPedido.Cantidad))
            {
                _pedidosTemporales[tempPedido.Id] = tempPedido;
                await Clients.All.SendAsync("ActualizarFilaTemporal", tempPedido);
            }
        }

        public async Task EliminarFilaTemporal(string idPedido)
        {
            if (_pedidosTemporales.ContainsKey(idPedido))
            {
                _pedidosTemporales.Remove(idPedido);
                await Clients.All.SendAsync("EliminarFilaTemporal", idPedido);
            }
        }

        public async Task ActualizarPedido(Pedido pedidoActualizado)
        {
            var pedido = _pedidos.FirstOrDefault(p => p.Id == pedidoActualizado.Id);
            if (pedido != null)
            {
                pedido.PedidoNombre = pedidoActualizado.PedidoNombre;
                pedido.Cantidad = pedidoActualizado.Cantidad;
                pedido.Fecha = pedidoActualizado.Fecha;
                pedido.Estatus = "En Revisión";

                await Clients.All.SendAsync("ActualizarPedidos", _pedidos);
            }
        }

        public async Task ActualizarPedidos()
        {
            var todosLosPedidos = _pedidos.Concat(_pedidosTemporales.Values).ToList();
            await Clients.All.SendAsync("ActualizarPedidos", todosLosPedidos);
        }

        public async Task MarcarPedidoEnEdicion(string idPedido)
        {
            await Clients.All.SendAsync("MarcarPedidoEnEdicion", idPedido);
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
}