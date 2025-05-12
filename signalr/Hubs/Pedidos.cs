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
        private static Dictionary<string, string> _conexionIdAIdTemporal = new Dictionary<string, string>();
        private static Dictionary<string, int> _secuenciaPorTipo = new Dictionary<string, int>()
        {
            { "D1", 0 },
            { "D2", 0 },
            { "D3", 0 },
            { "D4", 0 },
            { "D5", 0 }
        };



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

            // Si existe un pedido temporal con el mismo idTemporal, lo eliminamos primero
            if (_pedidosTemporales.ContainsKey(idTemporal))
            {
                _pedidosTemporales.Remove(idTemporal);
                await Clients.Group(nuevoPedido.TipoPedido).SendAsync("EliminarFilaTemporal", idTemporal);
            }

            // Asignar el número secuencial en base al tipo de pedido
            if (_secuenciaPorTipo.ContainsKey(nuevoPedido.TipoPedido))
            {
                _secuenciaPorTipo[nuevoPedido.TipoPedido]++;
                nuevoPedido.NumeroSecuencial = _secuenciaPorTipo[nuevoPedido.TipoPedido];
            }

            // Agregar el pedido a la lista global de pedidos confirmados
            _pedidos.Add(nuevoPedido);

            // Notificar a todos los clientes del grupo que se confirmó el pedido
            await Clients.Group(nuevoPedido.TipoPedido).SendAsync("PedidoConfirmado", nuevoPedido);

            // Si es D1 o D2, calcular y notificar el contador actualizado
            if (nuevoPedido.TipoPedido == "D1" || nuevoPedido.TipoPedido == "D2")
            {
                int restantes = ObtenerContadorRestante(nuevoPedido.TipoPedido);
                await Clients.Group(nuevoPedido.TipoPedido).SendAsync("ActualizarContador", restantes);
            }
        }

        public async Task EliminarPedido(string usuario, string idPedido, string tipoPedido)
        {
            // Validación de parámetros
            if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(idPedido) || string.IsNullOrEmpty(tipoPedido))
            {
                Console.WriteLine("❌ Error: Parámetros inválidos en EliminarPedido");
                return;
            }

            // Buscar el pedido sin filtrar por usuario para evitar problemas
            var pedido = _pedidos.FirstOrDefault(p => p.Id == idPedido);

            if (pedido == null)
            {
                Console.WriteLine($"⚠️ No se encontró el pedido con ID: {idPedido}");
                return;
            }

            // Verificar si el usuario tiene permisos para eliminar el pedido
            if (pedido.Usuario != usuario)
            {
                Console.WriteLine($"❌ Error: Usuario '{usuario}' intentó eliminar un pedido que no le pertenece.");
                return;
            }

            // Eliminar el pedido de la lista
            _pedidos.Remove(pedido);
            Console.WriteLine($"✅ Pedido {idPedido} eliminado por usuario {usuario}");

            // Notificar solo a los usuarios del grupo correspondiente
            await Clients.Group(tipoPedido).SendAsync("PedidoEliminado", idPedido);

            // Si el tipo es D1 o D2, actualizar el contador de filas restantes
            if (tipoPedido == "D1" || tipoPedido == "D2")
            {
                int restantes = ObtenerContadorRestante(tipoPedido);
                await Clients.Group(tipoPedido).SendAsync("ActualizarContador", restantes);
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
            _conexionIdAIdTemporal[Context.ConnectionId] = tempPedido.Id; // Asocia la conexión con el id temporal


            // ✅ Enviar solo a los usuarios del grupo correspondiente
            await Clients.Group(tempPedido.TipoPedido).SendAsync("ActualizarFilaTemporal", tempPedido);
        }

        public async Task EliminarFilaTemporal(string idTemporal, string tipoPedido)
        {
            if (_pedidosTemporales.ContainsKey(idTemporal))
            {
                _pedidosTemporales.Remove(idTemporal);
                Console.WriteLine($"🗑 Pedido temporal eliminado correctamente: {idTemporal}");
            }
            else
            {
                Console.WriteLine($"⚠️ No se encontró la fila temporal con ID: {idTemporal}");
            }

            // Notificar a todos los usuarios del grupo para eliminar la fila en la vista
            await Clients.Group(tipoPedido).SendAsync("EliminarFilaTemporal", idTemporal);
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

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            string connectionId = Context.ConnectionId;

            // En este ejemplo asumiremos que tienes un diccionario que mapea connectionId a idTemporal:
            if (_conexionIdAIdTemporal.TryGetValue(connectionId, out string idTemporal))
            {
                if (_pedidosTemporales.ContainsKey(idTemporal))
                {
                    _pedidosTemporales.Remove(idTemporal);
                    // Notificar a todos los clientes del grupo (o a todos) para eliminar la fila temporal.
                    await Clients.All.SendAsync("EliminarFilaTemporal", idTemporal);
                }
                // Eliminar el mapeo para evitar fugas de memoria.
                _conexionIdAIdTemporal.Remove(connectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        private int ObtenerContadorRestante(string tipoPedido)
        {
            int limite = (tipoPedido == "D1") ? 10 : (tipoPedido == "D2") ? 20 : int.MaxValue;
            int count = _pedidos.Where(p => p.TipoPedido == tipoPedido).Count() +
                        _pedidosTemporales.Values.Where(p => p.TipoPedido == tipoPedido).Count();
            return Math.Max(limite - count, 0);
        }

        public async Task ObtenerTodosLosPedidos()
        {
            var todos = _pedidos.Concat(_pedidosTemporales.Values).ToList();
            await Clients.Caller.SendAsync("ActualizarPedidos", todos);
        }

        public async Task EliminarPedidoAdmin(string idPedido)
        {
            // Localiza el pedido en _pedidos, sin validación de usuario
            var pedido = _pedidos.FirstOrDefault(p => p.Id == idPedido);
            if (pedido == null) return;

            _pedidos.Remove(pedido);

            // Notificar al grupo correspondiente
            await Clients.Group(pedido.TipoPedido).SendAsync("PedidoEliminado", idPedido);
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

            // Agregamos un número secuencial para cada pedido
            public int NumeroSecuencial { get; set; }

        }

    }
}