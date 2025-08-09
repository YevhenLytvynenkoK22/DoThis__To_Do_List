cmake_minimum_required(VERSION 3.15)
project(ToDoServer)

find_package(gRPC CONFIG  REQUIRED)
find_package(Protobuf  CONFIG REQUIRED)
find_package(ZLIB  REQUIRED)
find_package(OpenSSL  REQUIRED)

add_executable(server src/server.cpp generated/todo.pb.cc generated/todo.grpc.pb.cc)

target_include_directories(server PRIVATE ${Protobuf_INCLUDE_DIRS} ${CMAKE_CURRENT_SOURCE_DIR}/generated)
target_link_libraries(server PRIVATE gRPC::grpc++ protobuf::libprotobuf)
