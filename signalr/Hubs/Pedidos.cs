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

        public async Task CambiarGrupo(string tipoPedido)
        {
            string connectionId = Context.ConnectionId;

            // Remover de todos los grupos previos
            await Groups.RemoveFromGroupAsync(connectionId, "D1");
            await Groups.RemoveFromGroupAsync(connectionId, "D2");
            await Groups.RemoveFromGroupAsync(connectionId, "D3");
            await Groups.RemoveFromGroupAsync(connectionId, "D4");
            await Groups.RemoveFromGroupAsync(connectionId, "D5");

            // Agregar al nuevo grupo
            await Groups.AddToGroupAsync(connectionId, tipoPedido);
            await Clients.Caller.SendAsync("GrupoCambiado", tipoPedido);
        }


        public async Task EnviarPedido(Pedido nuevoPedido)
        {
            nuevoPedido.Estatus = "En revisión";
            string idTemporal = $"temp-{nuevoPedido.Usuario}";

            if (_pedidosTemporales.ContainsKey(idTemporal))
            {
                _pedidosTemporales.Remove(idTemporal);
                await Clients.Group(nuevoPedido.TipoPedido).SendAsync("EliminarFilaTemporal", idTemporal);
            }

            _pedidos.Add(nuevoPedido);

            // ✅ Ahora solo lo enviamos al grupo correspondiente
            await Clients.Group(nuevoPedido.TipoPedido).SendAsync("PedidoConfirmado", nuevoPedido);
        }

        public async Task EliminarPedido(string usuario, string idPedido, string tipoPedido)
        {
            var pedido = _pedidos.FirstOrDefault(p => p.Id == idPedido && p.Usuario == usuario);
            if (pedido != null)
            {
                _pedidos.Remove(pedido);
                await Clients.Group(tipoPedido).SendAsync("PedidoEliminado", idPedido);
            }
        }

        public async Task ObtenerPedidos(string tipoPedido)
        {
            var pedidosFiltrados = _pedidos
                .Where(p => p.TipoPedido == tipoPedido)
                .Concat(_pedidosTemporales.Values.Where(p => p.TipoPedido == tipoPedido))
                .ToList();

            await Clients.Caller.SendAsync("ActualizarPedidos", pedidosFiltrados);
        }

        public async Task ActualizarFilaTemporal(Pedido tempPedido)
        {
            _pedidosTemporales[tempPedido.Id] = tempPedido;

            // ✅ Enviar solo a los usuarios del grupo correspondiente
            await Clients.Group(tempPedido.TipoPedido).SendAsync("ActualizarFilaTemporal", tempPedido);
        }

        public async Task EliminarFilaTemporal(string idPedido, string tipoPedido)
        {
            if (_pedidosTemporales.ContainsKey(idPedido))
            {
                _pedidosTemporales.Remove(idPedido);
                await Clients.Group(tipoPedido).SendAsync("EliminarFilaTemporal", idPedido);
            }
        }

        public async Task ActualizarPedido(Pedido pedidoActualizado)
        {
            var pedido = _pedidos.FirstOrDefault(p => p.Id == pedidoActualizado.Id);
            if (pedido != null)
            {
                pedido.ClavePedido = pedidoActualizado.ClavePedido;  // ✅ Ahora se actualiza la Clave del Pedido
                pedido.PedidoNombre = pedidoActualizado.PedidoNombre;
                pedido.Cantidad = pedidoActualizado.Cantidad;
                pedido.NumeroPedido = pedidoActualizado.NumeroPedido; // ✅ Ahora se actualiza el Número de Pedido
                pedido.Observaciones = pedidoActualizado.Observaciones; // ✅ Ahora se actualizan las Observaciones
                pedido.Fecha = pedidoActualizado.Fecha;
                pedido.Estatus = "En Revisión";

                Console.WriteLine($"🔄 Pedido Actualizado: {pedido.Id}, Clave: {pedido.ClavePedido}, Número Pedido: {pedido.NumeroPedido}, Observaciones: {pedido.Observaciones}");

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
            public string ClavePedido { get; set; }
            public string PedidoNombre { get; set; }
            public string Cantidad { get; set; }
            public string NumeroPedido { get; set; }
            public string Observaciones { get; set; }
            public string Fecha { get; set; }
            public string Usuario { get; set; }
            public string Estatus { get; set; }
            public string TipoPedido { get; set; }

        }

    }
}