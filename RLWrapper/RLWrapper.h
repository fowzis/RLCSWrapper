//
// RLWrapper.h
// C-compatible wrapper for RL library trajectory planning
//

#ifndef RL_WRAPPER_H
#define RL_WRAPPER_H

#ifdef _WIN32
    #ifdef RLWRAPPER_EXPORTS
        #define RL_PLANNER_API __declspec(dllexport)
    #else
        #define RL_PLANNER_API __declspec(dllimport)
    #endif
#else
    #define RL_PLANNER_API __attribute__((visibility("default")))
#endif

#ifdef __cplusplus
extern "C" {
#endif

// Error codes
#define RL_SUCCESS 0
#define RL_ERROR_INVALID_POINTER -1
#define RL_ERROR_INVALID_PARAMETER -2
#define RL_ERROR_LOAD_FAILED -3
#define RL_ERROR_PLANNING_FAILED -4
#define RL_ERROR_NOT_INITIALIZED -5
#define RL_ERROR_EXCEPTION -6

// Create planner instance - maintains scene and kinematics for lifetime
RL_PLANNER_API void* CreatePlanner();

// Load kinematics ONCE - stored in planner instance
// Returns RL_SUCCESS (0) on success, negative error code on failure
RL_PLANNER_API int LoadKinematics(void* planner, const char* xmlPath);

// Load scene with obstacles ONCE - stored in planner instance
// Scene includes robot model and all obstacles for collision checking
// Returns RL_SUCCESS (0) on success, negative error code on failure
RL_PLANNER_API int LoadScene(void* planner, const char* xmlPath, int robotModelIndex);

// Load plan XML file that references kinematics and scene XMLs (like rlPlanDemo)
// Parses plan XML to extract scene path, kinematics path, robot model index, planner type, and parameters
// Also extracts start/goal configurations if present in XML
// Returns RL_SUCCESS (0) on success, negative error code on failure
RL_PLANNER_API int LoadPlanXml(void* planner, const char* xmlPath);

// Set start configuration - stored in planner instance for reuse
// Returns RL_SUCCESS (0) on success, negative error code on failure
RL_PLANNER_API int SetStartConfiguration(void* planner, const double* config, int configSize);

// Set goal configuration - stored in planner instance for reuse
// Returns RL_SUCCESS (0) on success, negative error code on failure
RL_PLANNER_API int SetGoalConfiguration(void* planner, const double* config, int configSize);

// Plan trajectory - uses pre-loaded scene and kinematics
// Automatically checks collisions against scene obstacles
// waypoints: output buffer for waypoints (flattened: waypointCount * dof values)
// maxWaypoints: maximum number of waypoints that can be stored
// waypointCount: output - actual number of waypoints returned
// Returns RL_SUCCESS (0) on success, negative error code on failure
RL_PLANNER_API int PlanTrajectory(
    void* planner,
    const double* start, int startSize,
    const double* goal, int goalSize,
    int useZAxis, const char* plannerType,
    double delta, double epsilon, int timeoutMs,
    double* waypoints, int maxWaypoints, int* waypointCount);

// Check if configuration is collision-free (uses loaded scene)
// Returns 1 if valid (collision-free and within joint limits), 0 if invalid
RL_PLANNER_API int IsValidConfiguration(void* planner, const double* config, int configSize);

// Get degrees of freedom (number of joints)
// Returns DOF count, or negative error code on failure
RL_PLANNER_API int GetDof(void* planner);

// Cleanup - destroys scene and kinematics
RL_PLANNER_API void DestroyPlanner(void* planner);

#ifdef __cplusplus
}
#endif

#endif // RL_WRAPPER_H

