cmake_minimum_required(VERSION 3.10)
project(RLWrapper)

set(CMAKE_CXX_STANDARD 11)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Find RL library
find_package(rl REQUIRED)

# Find collision detection engines
find_package(Bullet QUIET)
find_package(FCL QUIET)
find_package(ODE QUIET)
find_package(PQP QUIET)
find_package(SOLID3 QUIET)

# Source files
set(SOURCES
    RLWrapper.cpp
)

set(HEADERS
    RLWrapper.h
)

# Create shared library
add_library(RLWrapper SHARED ${SOURCES} ${HEADERS})

# Include directories
target_include_directories(RLWrapper PUBLIC
    ${CMAKE_CURRENT_SOURCE_DIR}
)

# Link libraries
target_link_libraries(RLWrapper
    rl::plan
    rl::kin
    rl::mdl
    rl::sg
    rl::math
    rl::xml
    rl::util
)

# Platform-specific settings
if(WIN32)
    set_target_properties(RLWrapper PROPERTIES
        OUTPUT_NAME "RLWrapper"
        PREFIX ""
    )
else()
    set_target_properties(RLWrapper PROPERTIES
        OUTPUT_NAME "RLWrapper"
        PREFIX "lib"
    )
endif()

# Install
install(TARGETS RLWrapper
    LIBRARY DESTINATION lib
    ARCHIVE DESTINATION lib
    RUNTIME DESTINATION bin
)

install(FILES RLWrapper.h DESTINATION include)

