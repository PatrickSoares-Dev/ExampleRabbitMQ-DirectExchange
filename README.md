## 📘 Documentation

This project consists of two primary components:

### Publisher

- Generates fake `Pedido` (order) data.
- Publishes messages to the `pedido.exchange` using the `pedido.criado` routing key.

### Consumer

- Subscribes to the `pedido.criados` queue.
- Processes incoming order messages and logs them to the console.

---

## 📂 Models

This project uses two primary data models: `Pedido` and `Item`, which represent an order and its individual items, respectively.

---

## 🔁 Architecture

The project follows a basic **publish-subscribe** messaging architecture using **RabbitMQ** as the message broker.

---

### 🧩 Components

- **Exchange**: `pedido.exchange`  
  Type: `direct`  
  Responsible for routing messages to the appropriate queue based on the routing key.

- **Queue**: `pedido.criados`  
  Stores messages until they are consumed by the Consumer.

- **Routing Key**: `pedido.criado`  
  Used by the Publisher to route messages to the correct queue via the exchange.

---

### 🔄 Message Flow


### 🧾 Pedido

Represents a customer order with the following properties:

| Property         | Type           | Description                          |
|------------------|----------------|--------------------------------------|
| `Id`             | `Guid`         | Unique identifier for the order      |
| `ClienteEmail`   | `string`       | Email address of the customer        |
| `ValorTotal`     | `decimal`      | Total value of the order             |
| `DataCriacao`    | `DateTime`     | Timestamp of when the order was created |
| `Itens`          | `List<Item>`   | List of items included in the order  |

---

### 📦 Item

Represents a product item within an order:

| Property         | Type           | Description                            |
|------------------|----------------|----------------------------------------|
| `NomeProduto`    | `string`       | Name of the product                    |
| `Quantidade`     | `int`          | Quantity of the product ordered        |
| `PrecoUnitario`  | `decimal`      | Unit price of the product              |


## ⚙️ Configuration

By default, the application connects to RabbitMQ using:

- Host: `localhost`
- Port: `5672`
- Username: `guest`
- Password: `guest`

You may update the connection settings in your code or configuration file if needed.

---

## 🧪 Testing

To test the full flow:

1. Start RabbitMQ locally.
2. Run the **Consumer** to begin listening for messages.
3. Run the **Publisher** and enter the number of orders to send.
4. Observe the **Consumer** receiving and processing messages.
