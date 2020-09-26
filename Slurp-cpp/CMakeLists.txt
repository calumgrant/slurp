# CMakeList.txt : CMake project for Slurp-cpp, include source and define
# project specific logic here.
#
cmake_minimum_required (VERSION 3.8)

# Add source to this project's executable.
add_executable (Slurp-cpp "Slurp-cpp.cpp" "Slurp-cpp.h" "typeset.h" "Node.h" "Stack.hpp" "Stack.cpp" "Rules.hpp" "RulesTests.cpp" "typeset_tests.cpp" "is_empty.hpp" "slurp.hpp" "first.hpp" "follows.hpp" "parser_construction.hpp" "closure.hpp" "prettyprint.hpp")

# TODO: Add tests and install targets if needed.
