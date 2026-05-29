// hermes_cpp_learning.cpp
// C++ core for Hermes agent learning

#include <vector>
#include <cmath>

extern "C" {
// Example: simple skill training function
__declspec(dllexport) double train_skill(const double* data, int len) {
    double sum = 0;
    for (int i = 0; i < len; ++i) sum += data[i];
    return std::sqrt(sum);
}
}
