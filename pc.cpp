#include <iostream>
#include <queue>
#include <vector>
#include <mutex>
#include <condition_variable>
#include <thread>
#include <chrono>

template <typename T>
class ProducerConsumer {
public:
    explicit ProducerConsumer(size_t capacity) : capacity_(capacity), done_(false) {}

    void produce(const T& item, bool is_end = false) {
        std::unique_lock<std::mutex> lock(mutex_);
        cond_full_.wait(lock, [this]() { return buffer_.size() < capacity_; });

        buffer_.push(item);
        if (is_end) {
            done_ = true;
        }

        cond_empty_.notify_one();  // Notify consumers
    }

    std::vector<T> consume_all() {
        std::unique_lock<std::mutex> lock(mutex_);

        // Wait for items OR done
        cond_empty_.wait(lock, [this]() { return !buffer_.empty() || done_; });

        std::vector<T> ret;
        while (!buffer_.empty()) {
            ret.push_back(std::move(buffer_.front()));
            buffer_.pop();
        }

        cond_full_.notify_all();  // Wake producers if waiting
        return ret;
    }

    bool is_done() {
        std::lock_guard<std::mutex> lock(mutex_);
        return done_ && buffer_.empty();
    }

private:
    std::queue<T> buffer_;
    size_t capacity_;
    mutable std::mutex mutex_;
    std::condition_variable cond_full_;
    std::condition_variable cond_empty_;
    bool done_;
};

void producer_thread(ProducerConsumer<int>& pc) {
    for (int i = 0; i < 20; ++i) {
        bool is_last = (i == 19);
        pc.produce(i, is_last);
        std::cout << "Produced: " << i << std::endl;
        std::this_thread::sleep_for(std::chrono::milliseconds(40));
    }
}

void consumer_thread(ProducerConsumer<int>& pc) {
    while (true) {
        auto items = pc.consume_all();
        for (int item : items) {
            std::cout << "Consumed: " << item << std::endl;
        }
        std::this_thread::sleep_for(std::chrono::milliseconds(1000));
        if (pc.is_done()) {
            break;
        }
    }
}


void test_pc() {
    ProducerConsumer<int> pc(100);

    std::thread th_consumer(consumer_thread, std::ref(pc));
    std::thread th_producer(producer_thread, std::ref(pc));
    

    th_producer.join();
    th_consumer.join();
}
