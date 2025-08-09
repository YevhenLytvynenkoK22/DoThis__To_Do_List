#include <iostream>
#include <memory>
#include <string>
#include <mutex>
#include <unordered_map>
#include <vector>
#include <thread>
#include <algorithm>
#include <optional>

#include <grpcpp/grpcpp.h>
#include "todo.pb.h"
#include "todo.grpc.pb.h"

using grpc::Server;
using grpc::ServerBuilder;
using grpc::ServerContext;
using grpc::ServerWriter;
using grpc::Status;

using todo::ToDoService;
using todo::Task;
using todo::Column;
using todo::Board;
using todo::AddColumnRequest;
using todo::RenameColumnRequest;
using todo::AddTaskToColumnRequest;
using todo::MoveTaskRequest;
using todo::UpdateTaskRequest;
using todo::ToggleTaskStateRequest; 
using todo::Empty;
using todo::ColumnIdRequest;
using todo::TaskIdRequest;

class ToDoServiceImpl final : public ToDoService::Service {
private:
    std::mutex data_mutex;
    std::unordered_map<int32_t, Column> columns;
    int32_t next_column_id = 0;
    int32_t next_task_id = 1;

    std::mutex stream_mutex;
    std::vector<ServerWriter<Board>*> active_streams;

    void BroadcastBoard() {
        Board board;
        {
            std::lock_guard<std::mutex> data_lock(data_mutex);
            for (const auto& pair : columns) {
                *board.add_columns() = pair.second;
            }
        }

        std::vector<ServerWriter<Board>*> dead_streams;
        {
            std::lock_guard<std::mutex> stream_lock(stream_mutex);
            for (auto* writer : active_streams) {
                try {
                    if (!writer->Write(board)) {
                        dead_streams.push_back(writer);
                    }
                } catch (const std::exception& ex) {
                    dead_streams.push_back(writer);
                    std::cerr << "[Broadcast] Failed to write with exception: " << ex.what() << std::endl;
                } catch (...) {
                    dead_streams.push_back(writer);
                    std::cerr << "[Broadcast] Failed to write with unknown exception." << std::endl;
                }
            }

            for (auto* dead_writer : dead_streams) {
                auto it = std::remove(active_streams.begin(), active_streams.end(), dead_writer);
                if (it != active_streams.end()) {
                    active_streams.erase(it, active_streams.end());
                }
            }
        }
    }

public:
    Status AddColumn(ServerContext* context, const AddColumnRequest* request, Column* response) override {
        std::cout << "[AddColumn] Request received: name = " << request->name() << std::endl;

        if (request->name().empty()) {
            return Status(grpc::StatusCode::INVALID_ARGUMENT, "Column name is empty");
        }

        {
            std::lock_guard<std::mutex> lock(data_mutex);
            Column col;
            col.set_id(next_column_id++);
            col.set_name(request->name());
            columns[col.id()] = col;
            *response = col;
        }

        try {
            BroadcastBoard();
        } catch (...) {
            std::cerr << "[AddColumn] ERROR during BroadcastBoard()." << std::endl;
            return Status(grpc::StatusCode::INTERNAL, "Error during broadcast.");
        }
        return Status::OK;
    }

    Status RenameColumn(ServerContext* context, const RenameColumnRequest* request, Column* response) override {
        std::cout << "[RenameColumn] Request received: id = " << request->column_id() << ", name = " << request->new_name() << std::endl;

        {
            std::lock_guard<std::mutex> lock(data_mutex);
            auto it = columns.find(request->column_id());
            if (it == columns.end()) {
                return Status(grpc::StatusCode::NOT_FOUND, "Column not found");
            }
            it->second.set_name(request->new_name());
            *response = it->second;
        }

        try {
            BroadcastBoard();
        } catch (...) {
            std::cerr << "[RenameColumn] ERROR during BroadcastBoard()." << std::endl;
            return Status(grpc::StatusCode::INTERNAL, "Error during broadcast.");
        }
        return Status::OK;
    }

    Status AddTaskToColumn(ServerContext* context, const AddTaskToColumnRequest* request, Task* response) override {
        std::cout << "[AddTaskToColumn] Request received: column_id = " << request->column_id() << ", title = " << request->title() << std::endl;

        {
            std::lock_guard<std::mutex> lock(data_mutex);
            auto it = columns.find(request->column_id());
            if (it == columns.end()) {
                return Status(grpc::StatusCode::NOT_FOUND, "Column not found");
            }
            Task task;
            task.set_id(next_task_id++);
            task.set_title(request->title());
            task.set_description(request->description());
            task.set_state(todo::Pending);
            *it->second.add_tasks() = task;
            *response = task;
        }

        try {
            BroadcastBoard();
        } catch (...) {
            std::cerr << "[AddTaskToColumn] ERROR during BroadcastBoard()." << std::endl;
            return Status(grpc::StatusCode::INTERNAL, "Error during broadcast.");
        }
        return Status::OK;
    }

    Status UpdateTask(ServerContext* context, const UpdateTaskRequest* request, Task* response) override {
        std::cout << "[UpdateTask] Request received: task_id = " << request->task_id() << ", title = " << request->new_title() << std::endl;

        bool found = false;
        {
            std::lock_guard<std::mutex> lock(data_mutex);
            for (auto& pair : columns) {
                Column& column = pair.second;
                for (int i = 0; i < column.tasks_size(); ++i) {
                    Task* task = column.mutable_tasks(i);
                    if (task->id() == request->task_id()) {
                        task->set_title(request->new_title());
                        task->set_description(request->new_description());
                        task->set_state(request->new_state());
                        *response = *task;
                        found = true;
                        break;
                    }
                }
                if (found) break;
            }
        }

        if (!found) {
            return Status(grpc::StatusCode::NOT_FOUND, "Task not found");
        }

        try {
            BroadcastBoard();
        } catch (...) {
            std::cerr << "[UpdateTask] ERROR during BroadcastBoard()." << std::endl;
            return Status(grpc::StatusCode::INTERNAL, "Error during broadcast.");
        }
        return Status::OK;
    }

    Status ToggleTaskState(ServerContext* context, const ToggleTaskStateRequest* request, Task* response) override {
        std::cout << "[ToggleTaskState] Request received: task_id = " << request->task_id() << std::endl;

        bool found = false;
        {
            std::lock_guard<std::mutex> lock(data_mutex);
            for (auto& pair : columns) {
                Column& column = pair.second;
                for (int i = 0; i < column.tasks_size(); ++i) {
                    Task* task = column.mutable_tasks(i);
                    if (task->id() == request->task_id()) {
                        if (task->state() == todo::Pending) {
                            task->set_state(todo::Completed);
                        } else {
                            task->set_state(todo::Pending);
                        }
                        *response = *task;
                        found = true;
                        break;
                    }
                }
                if (found) break;
            }
        }

        if (!found) {
            return Status(grpc::StatusCode::NOT_FOUND, "Task not found");
        }

        try {
            BroadcastBoard();
        } catch (...) {
            std::cerr << "[ToggleTaskState] ERROR during BroadcastBoard()." << std::endl;
            return Status(grpc::StatusCode::INTERNAL, "Error during broadcast.");
        }
        return Status::OK;
    }

    Status MoveTask(ServerContext* context, const MoveTaskRequest* request, Task* response) override {
        std::cout << "[MoveTask] Request received: task_id = " << request->task_id() << ", from_col = " << request->from_column_id() << ", to_col = " << request->to_column_id() << std::endl;

        std::optional<Task> found_task_data;
        {
            std::lock_guard<std::mutex> lock(data_mutex);
            auto from_it = columns.find(request->from_column_id());
            auto to_it = columns.find(request->to_column_id());

            if (from_it == columns.end() || to_it == columns.end()) {
                return Status(grpc::StatusCode::NOT_FOUND, "Column not found");
            }

            Column& from_col = from_it->second;
            for (int i = 0; i < from_col.tasks_size(); ++i) {
                if (from_col.tasks(i).id() == request->task_id()) {
                    found_task_data = from_col.tasks(i);
                    from_col.mutable_tasks()->DeleteSubrange(i, 1);
                    break;
                }
            }

            if (!found_task_data.has_value()) {
                return Status(grpc::StatusCode::NOT_FOUND, "Task not found in from_column");
            }

            *to_it->second.add_tasks() = found_task_data.value();
            *response = found_task_data.value();
        }

        try {
            BroadcastBoard();
        } catch (...) {
            std::cerr << "[MoveTask] ERROR during BroadcastBoard()." << std::endl;
            return Status(grpc::StatusCode::INTERNAL, "Error during broadcast.");
        }
        return Status::OK;
    }

    Status GetBoard(ServerContext* context, const Empty* request, Board* response) override {
        std::lock_guard<std::mutex> lock(data_mutex);
        for (const auto& pair : columns) {
            *response->add_columns() = pair.second;
        }
        return Status::OK;
    }

    Status Sync(ServerContext* context, const Empty* request, ServerWriter<Board>* writer) override {
        {
            std::lock_guard<std::mutex> lock(stream_mutex);
            active_streams.push_back(writer);
            std::cout << "[Sync] Writer added to active_streams." << std::endl;
        }

        Board initial_board;
        {
            std::lock_guard<std::mutex> data_lock(data_mutex);
            for (const auto& pair : columns) {
                *initial_board.add_columns() = pair.second;
            }
        }
        if (!writer->Write(initial_board)) {
             std::lock_guard<std::mutex> lock(stream_mutex);
             auto it = std::find(active_streams.begin(), active_streams.end(), writer);
             if (it != active_streams.end()) {
                 active_streams.erase(it);
                 std::cout << "[Sync] Writer removed due to initial write failure." << std::endl;
             }
             return Status(grpc::StatusCode::ABORTED, "Failed to send initial board state.");
        }

        while (!context->IsCancelled()) {
            std::this_thread::sleep_for(std::chrono::seconds(1));
        }

        {
            std::lock_guard<std::mutex> lock(stream_mutex);
            auto it = std::find(active_streams.begin(), active_streams.end(), writer);
            if (it != active_streams.end()) {
                active_streams.erase(it);
                std::cout << "[Sync] Writer removed from active_streams." << std::endl;
            }
        }
        return Status::OK;
    }

    Status DeleteColumn(ServerContext* context, const ColumnIdRequest* request, Empty* response) override {
        std::cout << "[DeleteColumn] Request received: id = " << request->column_id() << std::endl;

        {
            std::lock_guard<std::mutex> lock(data_mutex);
            auto it = columns.find(request->column_id());
            if (it == columns.end()) {
                return Status(grpc::StatusCode::NOT_FOUND, "Column not found");
            }
            columns.erase(it);
        }

        try {
            BroadcastBoard();
        } catch (...) {
            std::cerr << "[DeleteColumn] ERROR during BroadcastBoard()." << std::endl;
            return Status(grpc::StatusCode::INTERNAL, "Error during broadcast.");
        }
        return Status::OK;
    }

    Status DeleteTask(ServerContext* context, const TaskIdRequest* request, Empty* response) override {
        std::cout << "[DeleteTask] Request received: id = " << request->task_id() << std::endl;

        bool found = false;
        {
            std::lock_guard<std::mutex> lock(data_mutex);
            for (auto& pair : columns) {
                Column& column = pair.second;
                auto& tasks = *column.mutable_tasks();
                auto it = std::remove_if(tasks.begin(), tasks.end(),
                    [task_id = request->task_id()](const Task& task) {
                        return task.id() == task_id;
                    });

                if (it != tasks.end()) {
                    tasks.erase(it, tasks.end());
                    found = true;
                    break;
                }
            }
        }

        if (!found) {
            return Status(grpc::StatusCode::NOT_FOUND, "Task not found");
        }

        try {
            BroadcastBoard();
        } catch (...) {
            std::cerr << "[DeleteTask] ERROR during BroadcastBoard()." << std::endl;
            return Status(grpc::StatusCode::INTERNAL, "Error during broadcast.");
        }
        return Status::OK;
    }


    void InitDefaultColumns() {
        std::lock_guard<std::mutex> lock(data_mutex);
        Column col1, col2, col3;
        col1.set_id(next_column_id++);
        col1.set_name("Planned");

        col2.set_id(next_column_id++);
        col2.set_name("In Progress");

        col3.set_id(next_column_id++);
        col3.set_name("Done");

        columns[col1.id()] = col1;
        columns[col2.id()] = col2;
        columns[col3.id()] = col3;
    }
};

int main() {
    std::string address("0.0.0.0:50051");
    ToDoServiceImpl service;
    service.InitDefaultColumns();
    ServerBuilder builder;
    builder.AddListeningPort(address, grpc::InsecureServerCredentials());
    builder.RegisterService(&service);
    std::unique_ptr<Server> server(builder.BuildAndStart());
    std::cout << "Server listening on " << address << std::endl;
    server->Wait();
    return 0;
}