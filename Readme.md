# Client-Server "To-Do List" Application  
C++ gRPC Server + C# WPF Client  

This project is a simple client-server application that allows multiple users to collaboratively manage a shared to-do list.  
The **server** is implemented in **C++** using **gRPC**.  
The **client** is implemented in **C#** with **WPF** and connects to the server to display and update the task list in real time.

---

## 📋 Features (From ToR)
- **Add Item**: Create a new to-do item with only a description; the server assigns a unique ID and returns the complete item.
- **Update Status**: Toggle the status of an existing to-do item (Pending/Completed) by ID.
- **Get List**: Retrieve the entire current list of items.
- **Real-Time Sync**: When one client makes a change, all other clients are notified immediately without manual refresh.
- **In-Memory Storage**: Server holds the list in memory (ID, description, status).

---

## 📦 Requirements

### Server (C++)
- **CMake** >= 3.15  
- **C++17** or newer  
- **gRPC** and **Protocol Buffers** (via vcpkg)  
- **vcpkg** (recommended for dependencies)  
- **Visual Studio 2022** (Windows) or g++/clang (Linux/Mac)  

### Client (C# WPF)
- **.NET 6.0** or newer  
- **Grpc.Net.Client**  
- **Google.Protobuf**  
- **Grpc.Tools**  
- **Visual Studio 2022** with WPF support  

---

## 🛠 Installing Dependencies

### 1. Install vcpkg

git clone https://github.com/microsoft/vcpkg.git    
cd vcpkg                             
bootstrap-vcpkg.bat   # Windows                
./bootstrap-vcpkg.sh # Linux/Mac

Add vcpkg to PATH or pass its path to CMake via -DCMAKE_TOOLCHAIN_FILE.

### 2. Install gRPC and Protobuf

- vcpkg install grpc:x64-windows
- vcpkg install protobuf:x64-windows

On Linux, replace x64-windows with your platform (e.g., x64-linux).

## 📄 Compiling the .proto File

- protoc -I=proto --grpc_out=server --plugin=protoc-gen-grpc="C:/path/to/grpc_cpp_plugin.exe" proto/todo.proto
- protoc -I=proto --cpp_out=server proto/todo.proto


### 3. Setting the IP address of the server at the client
In the MainViewModel.cs file, in the ServerIP field, set the IP of your host

> private static string ServerIP = "26.225.145.107"; //Your local host IP (I use Radmin VPN for creating local network)  
> private static readonly GrpcChannel _channel = GrpcChannel.ForAddress($"http://{ServerIP}:50051");
> private readonly ToDoService.ToDoServiceClient _client = new ToDoService.ToDoServiceClient(_channel);


