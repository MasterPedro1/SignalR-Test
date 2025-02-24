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

            // Imprimir en consola los datos del pedido (incluyendo el tipo)
            Console.WriteLine("Nuevo pedido enviado:");
            Console.WriteLine($"ID: {nuevoPedido.Id}");
            Console.WriteLine($"ClavePedido: {nuevoPedido.ClavePedido}");
            Console.WriteLine($"PedidoNombre: {nuevoPedido.PedidoNombre}");
            Console.WriteLine($"Cantidad: {nuevoPedido.Cantidad}");
            Console.WriteLine($"NumeroPedido: {nuevoPedido.NumeroPedido}");
            Console.WriteLine($"Observaciones: {nuevoPedido.Observaciones}");
            Console.WriteLine($"Fecha: {nuevoPedido.Fecha}");
            Console.WriteLine($"Usuario: {nuevoPedido.Usuario}");
            Console.WriteLine($"Estatus: {nuevoPedido.Estatus}");
            Console.WriteLine($"TipoPedido: {nuevoPedido.TipoPedido}");

            // Definir el ID temporal basado en el usuario
            string idTemporal = $"temp-{nuevoPedido.Usuario}";

            // Eliminar la versión temporal del pedido enviado
            if (_pedidosTemporales.ContainsKey(idTemporal))
            {
                _pedidosTemporales.Remove(idTemporal);
                await Clients.Group(nuevoPedido.TipoPedido).SendAsync("EliminarFilaTemporal", idTemporal);
            }

            _pedidos.Add(nuevoPedido);
            // Enviar el pedido confirmado solo al grupo correspondiente
            await Clients.Group(nuevoPedido.TipoPedido).SendAsync("PedidoConfirmado", nuevoPedido);
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
            // Verificar que se haya asignado un tipo de pedido
            if (string.IsNullOrEmpty(tempPedido.TipoPedido))
            {
                Console.WriteLine("Error en ActualizarFilaTemporal: 'TipoPedido' es null o vacío.");
                return;
            }

            if (!string.IsNullOrEmpty(tempPedido.PedidoNombre) ||
                !string.IsNullOrEmpty(tempPedido.Cantidad) ||
                !string.IsNullOrEmpty(tempPedido.ClavePedido) ||
                !string.IsNullOrEmpty(tempPedido.NumeroPedido) ||
                !string.IsNullOrEmpty(tempPedido.Observaciones))
            {
                _pedidosTemporales[tempPedido.Id] = new Pedido
                {
                    Id = tempPedido.Id,
                    ClavePedido = tempPedido.ClavePedido,
                    PedidoNombre = tempPedido.PedidoNombre,
                    Cantidad = tempPedido.Cantidad,
                    NumeroPedido = tempPedido.NumeroPedido,
                    Observaciones = tempPedido.Observaciones,
                    Fecha = tempPedido.Fecha,
                    Usuario = tempPedido.Usuario,
                    Estatus = "Pendiente",
                    TipoPedido = tempPedido.TipoPedido // Aseguramos que se asigne el tipo
                };

                Console.WriteLine($"📢 Pedido Temporal Actualizado: {tempPedido.Id}");
                await Clients.Group(tempPedido.TipoPedido).SendAsync("ActualizarFilaTemporal", tempPedido);
            }
        }

        public async Task EliminarFilaTemporal(string idPedido)
        {
            Console.WriteLine($"🔴 [SERVIDOR] Solicitud recibida para eliminar fila temporal: {idPedido}");

            if (_pedidosTemporales.TryGetValue(idPedido, out Pedido pedidoTemporal))
            {
                _pedidosTemporales.Remove(idPedido);
                Console.WriteLine($"✅ [SERVIDOR] Fila eliminada correctamente: {idPedido}");

                // Notificar solo a los clientes en el grupo correspondiente al tipo de pedido
                await Clients.Group(pedidoTemporal.TipoPedido).SendAsync("EliminarFilaTemporal", idPedido);
            }
            else
            {
                Console.WriteLine($"⚠️ [SERVIDOR] No se encontró la fila temporal con ID {idPedido}");
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

        public async Task UnirseGrupo(string grupo)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, grupo);
            Console.WriteLine($"Cliente {Context.ConnectionId} se unió al grupo {grupo}");
        }

        public async Task CambiarGrupo(string nuevoGrupo)
        {
            // Lista de grupos posibles (ajusta según tus tipos de pedido)
            string[] grupos = new string[] { "D1", "D2", "D3", "D4", "D5" };
            foreach (var grupo in grupos)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, grupo);
            }
            await Groups.AddToGroupAsync(Context.ConnectionId, nuevoGrupo);
            Console.WriteLine($"Cliente {Context.ConnectionId} ahora está en el grupo {nuevoGrupo}");
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
            public string TipoPedido { get; set; }  // <-- Nueva propiedad
        }

    }
}