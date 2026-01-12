//
// RLWrapper.cpp
// C-compatible wrapper implementation for RL library trajectory planning
//

#define RLWRAPPER_EXPORTS
#include "RLWrapper.h"

#include <chrono>
#include <climits>
#include <cmath>
#include <cstring>
#include <iostream>
#include <limits>
#include <memory>
#include <stdexcept>
#include <string>
#include <vector>

#include <rl/kin/Kinematics.h>
#include <rl/math/Vector.h>
#include <rl/mdl/Dynamic.h>
#include <rl/mdl/XmlFactory.h>
#include <rl/plan/KdtreeNearestNeighbors.h>
#include <rl/plan/LinearNearestNeighbors.h>
#include <rl/plan/Prm.h>
#include <rl/plan/Planner.h>
#include <rl/plan/RecursiveVerifier.h>
#include <rl/plan/Rrt.h>
#include <rl/plan/RrtCon.h>
#include <rl/plan/RrtConCon.h>
#include <rl/plan/RrtGoalBias.h>
#include <rl/plan/Sampler.h>
#include <rl/plan/SequentialVerifier.h>
#include <rl/plan/SimpleModel.h>
#include <rl/plan/SimpleOptimizer.h>
#include <rl/plan/UniformSampler.h>
#include <rl/plan/VectorList.h>
#include <rl/plan/Verifier.h>
#include <rl/plan/NearestNeighbors.h>
#include <rl/sg/Model.h>
#include <rl/sg/Scene.h>
#include <rl/sg/XmlFactory.h>
#include <rl/xml/Document.h>
#include <rl/xml/DomParser.h>
#include <rl/xml/Path.h>
#include <rl/xml/Stylesheet.h>
#include <rl/math/Constants.h>

#ifdef RL_SG_BULLET
#include <rl/sg/bullet/Scene.h>
#endif
#ifdef RL_SG_FCL
#include <rl/sg/fcl/Scene.h>
#endif
#ifdef RL_SG_ODE
#include <rl/sg/ode/Scene.h>
#endif
#ifdef RL_SG_PQP
#include <rl/sg/pqp/Scene.h>
#endif
#ifdef RL_SG_SOLID
#include <rl/sg/solid/Scene.h>
#endif

// Internal planner state structure
struct PlannerState
{
    std::shared_ptr<rl::sg::Scene> scene;
    std::shared_ptr<rl::kin::Kinematics> kinematics;
    std::shared_ptr<rl::mdl::Model> mdl;  // Keep model alive if it's a Dynamic model
    std::shared_ptr<rl::plan::SimpleModel> model;
    rl::sg::Model* robotModel;
    bool initialized;
    
    // Persistent planner components
    std::shared_ptr<rl::plan::Planner> planner;
    std::shared_ptr<rl::plan::Sampler> sampler;
    std::shared_ptr<rl::plan::Verifier> verifier;
    std::shared_ptr<rl::plan::NearestNeighbors> nearestNeighbors;
    std::shared_ptr<rl::plan::SimpleOptimizer> optimizer;
    
    // Stored start/goal configurations
    std::shared_ptr<rl::math::Vector> start;
    std::shared_ptr<rl::math::Vector> goal;
    
    // Planner type and parameters
    std::string plannerType;
    double delta;
    double epsilon;
    int timeoutMs;
    
    PlannerState() : robotModel(nullptr), initialized(false), delta(0.1), epsilon(0.001), timeoutMs(30000) {}
};

// Helper function to create scene based on available engines
static std::shared_ptr<rl::sg::Scene> createScene()
{
#ifdef RL_SG_FCL
    return std::make_shared<rl::sg::fcl::Scene>();
#elif defined(RL_SG_ODE)
    return std::make_shared<rl::sg::ode::Scene>();
#elif defined(RL_SG_PQP)
    return std::make_shared<rl::sg::pqp::Scene>();
#elif defined(RL_SG_BULLET)
    return std::make_shared<rl::sg::bullet::Scene>();
#elif defined(RL_SG_SOLID)
    return std::make_shared<rl::sg::solid::Scene>();
#else
    throw std::runtime_error("No collision detection engine available");
#endif
}

// Helper function to constrain Z-axis for 2D planning
static void constrainZAxis(rl::math::Vector& goal, const rl::math::Vector& start, int zAxisIndex)
{
    if (zAxisIndex >= 0 && zAxisIndex < static_cast<int>(goal.size()))
    {
        goal(zAxisIndex) = start(zAxisIndex);
    }
}

extern "C" {

RL_PLANNER_API void* CreatePlanner()
{
    try
    {
        PlannerState* state = new PlannerState();
        return static_cast<void*>(state);
    }
    catch (...)
    {
        return nullptr;
    }
}

RL_PLANNER_API int LoadKinematics(void* planner, const char* xmlPath)
{
    if (!planner || !xmlPath)
    {
        return RL_ERROR_INVALID_POINTER;
    }
    
    try
    {
        PlannerState* state = static_cast<PlannerState*>(planner);
        
        // Try to load as Dynamic model first
        try
        {
            rl::mdl::XmlFactory factory;
            std::shared_ptr<rl::mdl::Model> mdl = factory.create(xmlPath);
            
            if (std::shared_ptr<rl::mdl::Dynamic> dynamic = std::dynamic_pointer_cast<rl::mdl::Dynamic>(mdl))
            {
                // Dynamic model - store the model and get kinematics from it
                // Dynamic inherits from Kinematics, so we can cast it
                state->mdl = mdl;  // Keep the model alive
                state->kinematics = std::dynamic_pointer_cast<rl::kin::Kinematics>(dynamic);
                if (!state->kinematics)
                {
                    return RL_ERROR_LOAD_FAILED;
                }
                return RL_SUCCESS;
            }
        }
        catch (const std::exception&)
        {
            // Not a Dynamic model file, try loading as Kinematics directly
            // This is expected for kinematics-only XML files
        }
        
        // Load as Kinematics directly (fallback if not a Dynamic model)
        state->kinematics = std::shared_ptr<rl::kin::Kinematics>(
            rl::kin::Kinematics::create(xmlPath)
        );
        
        return RL_SUCCESS;
    }
    catch (const std::exception& e)
    {
        std::cerr << "LoadKinematics exception: " << e.what() << " for file: " << xmlPath << std::endl;
        return RL_ERROR_LOAD_FAILED;
    }
    catch (...)
    {
        std::cerr << "LoadKinematics unknown exception for file: " << xmlPath << std::endl;
        return RL_ERROR_EXCEPTION;
    }
}

RL_PLANNER_API int LoadScene(void* planner, const char* xmlPath, int robotModelIndex)
{
    if (!planner || !xmlPath)
    {
        return RL_ERROR_INVALID_POINTER;
    }
    
    try
    {
        PlannerState* state = static_cast<PlannerState*>(planner);
        
        // Create scene
        state->scene = createScene();
        
        // Load scene from XML file
        state->scene->load(xmlPath);
        
        // Get robot model from scene
        int numModels = static_cast<int>(state->scene->getNumModels());
        std::cerr << "LoadScene: Loaded scene with " << numModels << " models, requested index: " << robotModelIndex << std::endl;
        if (robotModelIndex < 0 || robotModelIndex >= numModels)
        {
            std::cerr << "LoadScene: Invalid robotModelIndex " << robotModelIndex << " (valid range: 0 to " << (numModels - 1) << ")" << std::endl;
            return RL_ERROR_INVALID_PARAMETER;
        }
        state->robotModel = state->scene->getModel(robotModelIndex);
        
        // Create planning model based on scene type
        if (rl::sg::DistanceScene* distanceScene = dynamic_cast<rl::sg::DistanceScene*>(state->scene.get()))
        {
            // DistanceScene not typically used for planning, fall back to SimpleModel
            state->model = std::make_shared<rl::plan::SimpleModel>();
        }
        else if (rl::sg::SimpleScene* simpleScene = dynamic_cast<rl::sg::SimpleScene*>(state->scene.get()))
        {
            state->model = std::make_shared<rl::plan::SimpleModel>();
        }
        else
        {
            return RL_ERROR_LOAD_FAILED;
        }
        
        // Connect kinematics to model if loaded
        if (state->kinematics)
        {
            // Check if kinematics is actually a Dynamic model
            if (rl::mdl::Dynamic* dynamic = dynamic_cast<rl::mdl::Dynamic*>(state->kinematics.get()))
            {
                state->model->mdl = dynamic;
                std::cerr << "LoadScene: Connected Dynamic model to planning model" << std::endl;
            }
            else
            {
                state->model->kin = state->kinematics.get();
                std::cerr << "LoadScene: Connected Kinematics to planning model" << std::endl;
            }
        }
        else
        {
            std::cerr << "LoadScene: WARNING - No kinematics loaded, model may not work correctly" << std::endl;
        }
        
        // Connect model to scene
        state->model->model = state->robotModel;
        state->model->scene = state->scene.get();
        
        // Verify model is properly set up
        if (!state->model->kin && !state->model->mdl)
        {
            std::cerr << "LoadScene: ERROR - Model has no kinematics or dynamic model set" << std::endl;
            return RL_ERROR_NOT_INITIALIZED;
        }
        
        if (!state->model->model || !state->model->scene)
        {
            std::cerr << "LoadScene: ERROR - Model has no robot model or scene set" << std::endl;
            return RL_ERROR_NOT_INITIALIZED;
        }
        
        std::cerr << "LoadScene: Model DOF: " << state->model->getDofPosition() << std::endl;
        
        state->initialized = true;
        
        return RL_SUCCESS;
    }
    catch (const std::exception& e)
    {
        std::cerr << "LoadScene exception: " << e.what() << " for file: " << xmlPath << std::endl;
        return RL_ERROR_LOAD_FAILED;
    }
    catch (...)
    {
        std::cerr << "LoadScene unknown exception for file: " << xmlPath << std::endl;
        return RL_ERROR_EXCEPTION;
    }
}

// Helper function to create planner based on type
static std::shared_ptr<rl::plan::Planner> createPlanner(
    const std::string& plannerType,
    std::shared_ptr<rl::plan::Sampler> sampler,
    std::shared_ptr<rl::plan::Verifier> verifier,
    std::shared_ptr<rl::plan::NearestNeighbors> nearestNeighbors,
    double delta,
    double epsilon)
{
    std::shared_ptr<rl::plan::Planner> planner;
    
    if (plannerType == "rrt" || plannerType == "RRT")
    {
        std::shared_ptr<rl::plan::Rrt> rrt = std::make_shared<rl::plan::Rrt>();
        rrt->delta = delta;
        rrt->epsilon = epsilon;
        rrt->sampler = sampler.get();
        rrt->setNearestNeighbors(nearestNeighbors.get(), 0);
        planner = rrt;
    }
    else if (plannerType == "rrtConnect" || plannerType == "RRTConnect" || 
             plannerType == "rrtConCon" || plannerType == "RRTConCon")
    {
        std::shared_ptr<rl::plan::RrtConCon> rrtConCon = std::make_shared<rl::plan::RrtConCon>();
        rrtConCon->delta = delta;
        rrtConCon->epsilon = epsilon;
        rrtConCon->sampler = sampler.get();
        rrtConCon->setNearestNeighbors(nearestNeighbors.get(), 0);
        planner = rrtConCon;
    }
    else if (plannerType == "rrtGoalBias" || plannerType == "RRTGoalBias")
    {
        std::shared_ptr<rl::plan::RrtGoalBias> rrtGoalBias = std::make_shared<rl::plan::RrtGoalBias>();
        rrtGoalBias->delta = delta;
        rrtGoalBias->epsilon = epsilon;
        rrtGoalBias->probability = 0.05;
        rrtGoalBias->sampler = sampler.get();
        rrtGoalBias->setNearestNeighbors(nearestNeighbors.get(), 0);
        planner = rrtGoalBias;
    }
    else if (plannerType == "prm" || plannerType == "PRM")
    {
        std::shared_ptr<rl::plan::Prm> prm = std::make_shared<rl::plan::Prm>();
        prm->degree = std::numeric_limits<std::size_t>::max();
        prm->k = 30;
        prm->radius = std::numeric_limits<rl::math::Real>::max();
        prm->sampler = sampler.get();
        prm->verifier = verifier.get();
        prm->setNearestNeighbors(nearestNeighbors.get());
        planner = prm;
    }
    
    return planner;
}

RL_PLANNER_API int LoadPlanXml(void* planner, const char* xmlPath)
{
    if (!planner || !xmlPath)
    {
        return RL_ERROR_INVALID_POINTER;
    }
    
    try
    {
        PlannerState* state = static_cast<PlannerState*>(planner);
        
        // Parse XML file
        rl::xml::DomParser parser;
        rl::xml::Document document = parser.readFile(xmlPath, "", XML_PARSE_NOENT | XML_PARSE_XINCLUDE);
        document.substitute(XML_PARSE_NOENT | XML_PARSE_XINCLUDE);
        
        // Handle XSLT stylesheets if present (like rlPlanDemo)
        if ("stylesheet" == document.getRootElement().getName() || "transform" == document.getRootElement().getName())
        {
            if ("1.0" == document.getRootElement().getProperty("version"))
            {
                if (document.getRootElement().hasNamespace() && 
                    "http://www.w3.org/1999/XSL/Transform" == document.getRootElement().getNamespace().getHref())
                {
                    rl::xml::Stylesheet stylesheet(document);
                    document = stylesheet.apply();
                }
            }
        }
        
        rl::xml::Path path(document);
        
        // Extract scene file path
        rl::xml::NodeSet modelScene = path.eval("(/rl/plan|/rlplan)//model/scene").getValue<rl::xml::NodeSet>();
        if (modelScene.empty())
        {
            std::cerr << "LoadPlanXml: No scene element found in XML" << std::endl;
            return RL_ERROR_LOAD_FAILED;
        }
        std::string modelSceneFilename = modelScene[0].getLocalPath(modelScene[0].getProperty("href"));
        
        // Extract kinematics file path
        rl::xml::NodeSet modelKinematics = path.eval("(/rl/plan|/rlplan)//model/kinematics").getValue<rl::xml::NodeSet>();
        if (modelKinematics.empty())
        {
            std::cerr << "LoadPlanXml: No kinematics element found in XML" << std::endl;
            return RL_ERROR_LOAD_FAILED;
        }
        std::string modelKinematicsFilename = modelKinematics[0].getLocalPath(modelKinematics[0].getProperty("href"));
        
        // Extract robot model index
        std::size_t robotModelIndex = path.eval("number((/rl/plan|/rlplan)//model/model)").getValue<std::size_t>(0);
        
        // Extract planner type from root element
        std::string plannerTypeStr = document.getRootElement().getName();
        if (plannerTypeStr == "rlplan" || plannerTypeStr == "plan")
        {
            // Get planner type from child elements
            rl::xml::NodeSet planners = path.eval("(/rl/plan|/rlplan)//rrtConCon|(/rl/plan|/rlplan)//rrt|(/rl/plan|/rlplan)//rrtGoalBias|(/rl/plan|/rlplan)//prm").getValue<rl::xml::NodeSet>();
            if (!planners.empty())
            {
                plannerTypeStr = planners[0].getName();
            }
            else
            {
                plannerTypeStr = "rrtConCon"; // Default
            }
        }
        
        // Extract planner parameters
        double delta = 1.0;
        double epsilon = 0.001;
        int timeoutMs = 120000; // Default 120 seconds
        
        if (path.eval("count((/rl/plan|/rlplan)//delta) > 0").getValue<bool>())
        {
            delta = path.eval("number((/rl/plan|/rlplan)//delta)").getValue<double>(delta);
            std::string unit = path.eval("string((/rl/plan|/rlplan)//delta/@unit)").getValue<std::string>();
            if (unit == "deg")
            {
                delta *= rl::math::constants::deg2rad;
            }
        }
        
        if (path.eval("count((/rl/plan|/rlplan)//epsilon) > 0").getValue<bool>())
        {
            epsilon = path.eval("number((/rl/plan|/rlplan)//epsilon)").getValue<double>(epsilon);
            std::string unit = path.eval("string((/rl/plan|/rlplan)//epsilon/@unit)").getValue<std::string>();
            if (unit == "deg")
            {
                epsilon *= rl::math::constants::deg2rad;
            }
        }
        
        if (path.eval("count((/rl/plan|/rlplan)//duration) > 0").getValue<bool>())
        {
            timeoutMs = static_cast<int>(path.eval("number((/rl/plan|/rlplan)//duration)").getValue<double>(timeoutMs) * 1000.0);
        }
        
        // Store planner parameters
        state->plannerType = plannerTypeStr;
        state->delta = delta;
        state->epsilon = epsilon;
        state->timeoutMs = timeoutMs;
        
        // Load kinematics
        int result = LoadKinematics(planner, modelKinematicsFilename.c_str());
        if (result != RL_SUCCESS)
        {
            return result;
        }
        
        // Load scene
        result = LoadScene(planner, modelSceneFilename.c_str(), static_cast<int>(robotModelIndex));
        if (result != RL_SUCCESS)
        {
            return result;
        }
        
        // Extract and store start/goal configurations if present
        if (path.eval("count((/rl/plan|/rlplan)//start/q) > 0").getValue<bool>())
        {
            rl::xml::NodeSet start = path.eval("(/rl/plan|/rlplan)//start/q").getValue<rl::xml::NodeSet>();
            state->start = std::make_shared<rl::math::Vector>(start.size());
            for (int i = 0; i < start.size(); ++i)
            {
                (*state->start)(i) = std::atof(start[i].getContent().c_str());
                if ("deg" == start[i].getProperty("unit"))
                {
                    (*state->start)(i) *= rl::math::constants::deg2rad;
                }
            }
        }
        
        if (path.eval("count((/rl/plan|/rlplan)//goal/q) > 0").getValue<bool>())
        {
            rl::xml::NodeSet goal = path.eval("(/rl/plan|/rlplan)//goal/q").getValue<rl::xml::NodeSet>();
            state->goal = std::make_shared<rl::math::Vector>(goal.size());
            for (int i = 0; i < goal.size(); ++i)
            {
                (*state->goal)(i) = std::atof(goal[i].getContent().c_str());
                if ("deg" == goal[i].getProperty("unit"))
                {
                    (*state->goal)(i) *= rl::math::constants::deg2rad;
                }
            }
        }
        
        // Create persistent planner components
        state->sampler = std::make_shared<rl::plan::UniformSampler>();
        state->sampler->model = state->model.get();
        
        state->verifier = std::make_shared<rl::plan::RecursiveVerifier>();
        state->verifier->delta = delta;
        state->verifier->model = state->model.get();
        
        state->nearestNeighbors = std::make_shared<rl::plan::LinearNearestNeighbors>(state->model.get());
        
        state->optimizer = std::make_shared<rl::plan::SimpleOptimizer>();
        state->optimizer->model = state->model.get();
        state->optimizer->verifier = state->verifier.get();
        
        // Create planner
        state->planner = createPlanner(plannerTypeStr, state->sampler, state->verifier, state->nearestNeighbors, delta, epsilon);
        if (!state->planner)
        {
            std::cerr << "LoadPlanXml: Failed to create planner of type: " << plannerTypeStr << std::endl;
            return RL_ERROR_LOAD_FAILED;
        }
        
        state->planner->model = state->model.get();
        state->planner->duration = std::chrono::milliseconds(timeoutMs);
        
        if (state->start)
        {
            state->planner->start = state->start.get();
        }
        if (state->goal)
        {
            state->planner->goal = state->goal.get();
        }
        
        std::cerr << "LoadPlanXml: Successfully loaded plan XML with planner type: " << plannerTypeStr << std::endl;
        
        return RL_SUCCESS;
    }
    catch (const std::exception& e)
    {
        std::cerr << "LoadPlanXml exception: " << e.what() << " for file: " << xmlPath << std::endl;
        return RL_ERROR_LOAD_FAILED;
    }
    catch (...)
    {
        std::cerr << "LoadPlanXml unknown exception for file: " << xmlPath << std::endl;
        return RL_ERROR_EXCEPTION;
    }
}

RL_PLANNER_API int SetStartConfiguration(void* planner, const double* config, int configSize)
{
    if (!planner || !config)
    {
        return RL_ERROR_INVALID_POINTER;
    }
    
    if (configSize <= 0)
    {
        return RL_ERROR_INVALID_PARAMETER;
    }
    
    try
    {
        PlannerState* state = static_cast<PlannerState*>(planner);
        
        if (!state->initialized || !state->model)
        {
            return RL_ERROR_NOT_INITIALIZED;
        }
        
        int dof = static_cast<int>(state->model->getDofPosition());
        if (configSize != dof)
        {
            return RL_ERROR_INVALID_PARAMETER;
        }
        
        // Create or update start vector
        if (!state->start || state->start->size() != dof)
        {
            state->start = std::make_shared<rl::math::Vector>(dof);
        }
        
        for (int i = 0; i < dof; ++i)
        {
            (*state->start)(i) = config[i];
        }
        
        // Validate configuration
        if (!state->model->isValid(*state->start))
        {
            return RL_ERROR_INVALID_PARAMETER;
        }
        
        // Update planner's start pointer if planner exists
        if (state->planner)
        {
            state->planner->start = state->start.get();
        }
        
        return RL_SUCCESS;
    }
    catch (const std::exception&)
    {
        return RL_ERROR_EXCEPTION;
    }
    catch (...)
    {
        return RL_ERROR_EXCEPTION;
    }
}

RL_PLANNER_API int SetGoalConfiguration(void* planner, const double* config, int configSize)
{
    if (!planner || !config)
    {
        return RL_ERROR_INVALID_POINTER;
    }
    
    if (configSize <= 0)
    {
        return RL_ERROR_INVALID_PARAMETER;
    }
    
    try
    {
        PlannerState* state = static_cast<PlannerState*>(planner);
        
        if (!state->initialized || !state->model)
        {
            return RL_ERROR_NOT_INITIALIZED;
        }
        
        int dof = static_cast<int>(state->model->getDofPosition());
        if (configSize != dof)
        {
            return RL_ERROR_INVALID_PARAMETER;
        }
        
        // Create or update goal vector
        if (!state->goal || state->goal->size() != dof)
        {
            state->goal = std::make_shared<rl::math::Vector>(dof);
        }
        
        for (int i = 0; i < dof; ++i)
        {
            (*state->goal)(i) = config[i];
        }
        
        // Validate configuration
        if (!state->model->isValid(*state->goal))
        {
            return RL_ERROR_INVALID_PARAMETER;
        }
        
        // Update planner's goal pointer if planner exists
        if (state->planner)
        {
            state->planner->goal = state->goal.get();
        }
        
        return RL_SUCCESS;
    }
    catch (const std::exception&)
    {
        return RL_ERROR_EXCEPTION;
    }
    catch (...)
    {
        return RL_ERROR_EXCEPTION;
    }
}

RL_PLANNER_API int PlanTrajectory(
    void* planner,
    const double* start, int startSize,
    const double* goal, int goalSize,
    int useZAxis, const char* plannerType,
    double delta, double epsilon, int timeoutMs,
    double* waypoints, int maxWaypoints, int* waypointCount)
{
    if (!planner || !waypoints || !waypointCount)
    {
        return RL_ERROR_INVALID_POINTER;
    }
    
    try
    {
        PlannerState* state = static_cast<PlannerState*>(planner);
        
        if (!state->initialized || !state->model)
        {
            return RL_ERROR_NOT_INITIALIZED;
        }
        
        int dof = static_cast<int>(state->model->getDofPosition());
        
        // Determine start/goal vectors - use parameters if provided, otherwise use stored
        rl::math::Vector* startVec = nullptr;
        rl::math::Vector* goalVec = nullptr;
        std::shared_ptr<rl::math::Vector> tempStart;
        std::shared_ptr<rl::math::Vector> tempGoal;
        
        if (start && startSize > 0)
        {
            if (startSize != dof)
            {
                return RL_ERROR_INVALID_PARAMETER;
            }
            tempStart = std::make_shared<rl::math::Vector>(dof);
            for (int i = 0; i < dof; ++i)
            {
                (*tempStart)(i) = start[i];
            }
            startVec = tempStart.get();
        }
        else if (state->start)
        {
            startVec = state->start.get();
        }
        else
        {
            return RL_ERROR_INVALID_PARAMETER; // No start configuration
        }
        
        if (goal && goalSize > 0)
        {
            if (goalSize != dof)
            {
                return RL_ERROR_INVALID_PARAMETER;
            }
            tempGoal = std::make_shared<rl::math::Vector>(dof);
            for (int i = 0; i < dof; ++i)
            {
                (*tempGoal)(i) = goal[i];
            }
            
            // Handle Z-axis constraint
            if (!useZAxis && dof >= 3 && startVec)
            {
                int zAxisIndex = dof - 1;
                constrainZAxis(*tempGoal, *startVec, zAxisIndex);
            }
            goalVec = tempGoal.get();
        }
        else if (state->goal)
        {
            goalVec = state->goal.get();
        }
        else
        {
            return RL_ERROR_INVALID_PARAMETER; // No goal configuration
        }
        
        // Use persistent planner if available, otherwise create new one
        std::shared_ptr<rl::plan::Planner> rlPlanner = state->planner;
        
        if (!rlPlanner)
        {
            // Create planner components if not already created
            if (!state->sampler)
            {
                state->sampler = std::make_shared<rl::plan::UniformSampler>();
                state->sampler->model = state->model.get();
            }
            
            if (!state->verifier)
            {
                state->verifier = std::make_shared<rl::plan::RecursiveVerifier>();
                state->verifier->delta = delta > 0 ? delta : state->delta;
                state->verifier->model = state->model.get();
            }
            
            if (!state->nearestNeighbors)
            {
                state->nearestNeighbors = std::make_shared<rl::plan::LinearNearestNeighbors>(state->model.get());
            }
            
            // Determine planner type
            std::string plannerTypeStr;
            if (plannerType && strlen(plannerType) > 0)
            {
                plannerTypeStr = plannerType;
            }
            else if (!state->plannerType.empty())
            {
                plannerTypeStr = state->plannerType;
            }
            else
            {
                plannerTypeStr = "rrtConCon"; // Default
            }
            
            // Use provided parameters or stored defaults
            double useDelta = delta > 0 ? delta : state->delta;
            double useEpsilon = epsilon > 0 ? epsilon : state->epsilon;
            int useTimeout = timeoutMs > 0 ? timeoutMs : state->timeoutMs;
            
            // Create planner
            rlPlanner = createPlanner(plannerTypeStr, state->sampler, state->verifier, state->nearestNeighbors, useDelta, useEpsilon);
            if (!rlPlanner)
            {
                return RL_ERROR_INVALID_PARAMETER;
            }
            
            rlPlanner->model = state->model.get();
            rlPlanner->duration = std::chrono::milliseconds(useTimeout);
            
            // Store planner for reuse
            state->planner = rlPlanner;
            state->plannerType = plannerTypeStr;
            state->delta = useDelta;
            state->epsilon = useEpsilon;
            state->timeoutMs = useTimeout;
        }
        
        // Update planner with current start/goal
        rlPlanner->start = startVec;
        rlPlanner->goal = goalVec;
        
        // Update timeout if provided
        if (timeoutMs > 0)
        {
            rlPlanner->duration = std::chrono::milliseconds(timeoutMs);
        }
        
        // Verify start and goal configurations
        if (!rlPlanner->verify())
        {
            return RL_ERROR_PLANNING_FAILED;
        }
        
        // Plan trajectory
        bool solved = rlPlanner->solve();
        
        if (!solved)
        {
            *waypointCount = 0;
            return RL_ERROR_PLANNING_FAILED;
        }
        
        // Get path
        rl::plan::VectorList path = rlPlanner->getPath();
        
        // Optimize path if optimizer is available
        if (state->optimizer)
        {
            state->optimizer->process(path);
        }
        else
        {
            // Create temporary optimizer if not available
            std::shared_ptr<rl::plan::SimpleOptimizer> optimizer = std::make_shared<rl::plan::SimpleOptimizer>();
            optimizer->model = state->model.get();
            optimizer->verifier = state->verifier.get();
            optimizer->process(path);
        }
        
        // Copy waypoints to output buffer
        int count = static_cast<int>(path.size());
        if (count > maxWaypoints)
        {
            count = maxWaypoints;
        }
        
        *waypointCount = count;
        
        int idx = 0;
        for (auto it = path.begin(); it != path.end() && idx < count; ++it, ++idx)
        {
            const rl::math::Vector& waypoint = *it;
            for (int j = 0; j < dof; ++j)
            {
                waypoints[idx * dof + j] = waypoint(j);
            }
        }
        
        return RL_SUCCESS;
    }
    catch (const std::exception&)
    {
        return RL_ERROR_PLANNING_FAILED;
    }
    catch (...)
    {
        return RL_ERROR_EXCEPTION;
    }
}

RL_PLANNER_API int IsValidConfiguration(void* planner, const double* config, int configSize)
{
    if (!planner || !config)
    {
        std::cerr << "IsValidConfiguration: Null planner or config pointer" << std::endl;
        std::cerr.flush();
        return 0;
    }
    
    try
    {
        PlannerState* state = static_cast<PlannerState*>(planner);
        
        if (!state->initialized || !state->model)
        {
            return 0;
        }
        
        // Check if kinematics is properly set
        if (!state->kinematics)
        {
            return 0;
        }
        
        // Verify model has kinematics or dynamic model set
        if (!state->model->kin && !state->model->mdl)
        {
            return 0;
        }
        
        // Check if scene and robot model are set
        if (!state->model->model || !state->model->scene)
        {
            return 0;
        }
        
        int dof = 0;
        try
        {
            dof = static_cast<int>(state->model->getDofPosition());
        }
        catch (const std::exception&)
        {
            return 0;
        }
        catch (...)
        {
            return 0;
        }
        
        if (configSize != dof)
        {
            return 0;
        }
        
        rl::math::Vector q(dof);
        for (int i = 0; i < dof; ++i)
        {
            q(i) = config[i];
        }
        
        // Check joint limits first
        bool isValidResult = false;
        try
        {
            isValidResult = state->model->isValid(q);
        }
        catch (const std::exception&)
        {
            return 0;
        }
        catch (...)
        {
            return 0;
        }
        
        if (!isValidResult)
        {
            return 0;
        }
        
        // Check collision using the verifier (if available)
        // The verifier is what the planner uses and handles collision checking properly
        try
        {
            // Create or use existing verifier for collision checking
            std::shared_ptr<rl::plan::Verifier> verifier = state->verifier;
            
            if (!verifier && state->model)
            {
                // Create a verifier if not already created
                verifier = std::make_shared<rl::plan::RecursiveVerifier>();
                verifier->model = state->model.get();
                verifier->delta = state->delta > 0 ? state->delta : 0.1; // Use stored delta or default
                state->verifier = verifier; // Store for future use
            }
            
            if (verifier && state->model)
            {
                // Create a path with the same configuration twice (start = end = q)
                // This represents a zero-length path at configuration q
                rl::plan::VectorList path;
                path.push_back(q);
                path.push_back(q); // Same point twice = zero-length path
                
                // Check if the path (configuration) is collision-free
                bool collisionFree = verifier->isValid(path);
                if (!collisionFree)
                {
                    return 0;
                }
                
                return 1;
            }
            else
            {
                // Fallback: If verifier creation fails, rely on isValid() result
                // Note: SimpleModel::isValid() may only check joint limits, not collision
                return isValidResult ? 1 : 0;
            }
        }
        catch (const std::exception&)
        {
            // If collision check fails, at least return joint limits result
            return isValidResult ? 1 : 0;
        }
        catch (...)
        {
            return isValidResult ? 1 : 0;
        }
    }
    catch (const std::exception& e)
    {
        std::cerr << "IsValidConfiguration: Exception: " << e.what() << std::endl;
        std::cerr.flush();
        return 0;
    }
    catch (...)
    {
        std::cerr << "IsValidConfiguration: Unknown exception" << std::endl;
        std::cerr.flush();
        return 0;
    }
}

RL_PLANNER_API int GetDof(void* planner)
{
    if (!planner)
    {
        return RL_ERROR_INVALID_POINTER;
    }
    
    try
    {
        PlannerState* state = static_cast<PlannerState*>(planner);
        
        if (!state->initialized || !state->model)
        {
            return RL_ERROR_NOT_INITIALIZED;
        }
        
        return static_cast<int>(state->model->getDofPosition());
    }
    catch (...)
    {
        return RL_ERROR_EXCEPTION;
    }
}

RL_PLANNER_API void DestroyPlanner(void* planner)
{
    if (planner)
    {
        PlannerState* state = static_cast<PlannerState*>(planner);
        delete state;
    }
}

} // extern "C"

