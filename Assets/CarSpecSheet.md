# Vehicle Specification Sheet
*Generated on: 2026-05-21 01:13:41*

## Chassis & Dynamics
---
### Base Dynamics
| Parameter | Value |
|---|---|
| **Total Mass** | 2050 |
| **Center Of Mass Offset** | X: 0.00 | Y: 0.45 | Z: 0.19 |
| **Inertia Tensor Box Size** | X: 1.90 | Y: 1.40 | Z: 5.00 |
| **Max Safe Torque Multiplier** | 150 |

### Steering
| Parameter | Value |
|---|---|
| **Max Steer Angle** | 38 |
| **Ackermann Inner Multiplier** | 1.08 |
| **Ackermann Outer Multiplier** | 0.92 |

### Anti-Roll Bars
| Parameter | Value |
|---|---|
| **Front Anti Roll** | 28000 |
| **Rear Anti Roll** | 12000 |

## Aerodynamics
---
### Aero Settings
| Parameter | Value |
|---|---|
| **Air Density** | 1.225 |
| **Frontal Area** | 2.4 |
| **Planform Area** | 9.5 |
| **Side Area** | 5.2 |
| **Base Drag Coef** | 0.39 |
| **Pitch Drag Sensitivity** | 0.005 |
| **Yaw Drag Sensitivity** | 0.02 |
| **Front Base Downforce Coef** | -0.02 |
| **Front Pitch Sensitivity** | 0.02 |
| **Rear Base Downforce Coef** | 0.05 |
| **Rear Pitch Sensitivity** | 0.015 |
| **Yaw Sideforce Sensitivity** | 0.05 |
| **Optimal Ride Height** | 0.15 |
| **Max Ground Effect Coef** | 0.05 |
| **Ground Effect Bias** | 0.5 |
| **Aero Center Offset** | X: 0.00 | Y: 0.60 | Z: 0.00 |

## Powertrain
---
### Engine
| Parameter | Value |
|---|---|
| **Torque Csv File** | File: Freak_engine_map |
| **Engine Inertia** | 0.35 |
| **Static Friction** | 25 |
| **Dynamic Friction** | 0.08 |
| **Throttle Smoothing** | 0.15 |
| **Induction** | FreakInductionData (InductionData) |
| **Idle R P M** | 800 |
| **Redline R P M** | 6500 |

### Forced Induction
| Parameter | Value |
|---|---|
| **Induction Type** | Supercharged |
| **Max Pressure** | 1.60 Bar |
| **Supercharger Parasitic Drag** | 60.0 Nm |

### Clutch
| Parameter | Value |
|---|---|
| **Max Torque Capacity** | 1500 |
| **Lock Threshold** | 2 |

### Transmission
| Parameter | Value |
|---|---|
| **Forward Gears** | [ 2.9, 2, 1.5, 1.2, 1, 0.85 ] |
| **Reverse Gear** | 2.9 |
| **Final Drive** | 3.09 |
| **Efficiency** | 0.82 |
| **Transmission Inertia** | 0.25 |

### Auto Controller
| Parameter | Value |
|---|---|
| **Has Torque Converter Creep** | Yes |
| **Idle Creep Engagement** | 0.08 |
| **Bite R P M** | 1000 |
| **Lock R P M** | 1800 |
| **Upshift R P M** | 6300 |
| **Downshift R P M** | 2800 |
| **Shift Duration** | 0.15 |

## Drivetrain
---
### Layout
| Parameter | Value |
|---|---|
| **Drive Type** | AWD |

### Front Differential
| Parameter | Value |
|---|---|
| **Diff Type** | Open |
| **Slip Tolerance** | 2 |
| **Gear Ratio** | 1 |
| **Inertia** | 0.1 |
| **Power Bias** | 0.5 |
| **Preload L S D** | 0.05 |
| **Locking Friction** | 30 |
| **Locking Stiffness** | 1800 |
| **Coast Locking Multiplier** | 0 |

### Center Differential
| Parameter | Value |
|---|---|
| **Diff Type** | LimitedSlip |
| **Slip Tolerance** | 2 |
| **Gear Ratio** | 1 |
| **Inertia** | 0.15 |
| **Power Bias** | 0.4 |
| **Preload L S D** | 0.15 |
| **Locking Friction** | 1500 |
| **Locking Stiffness** | 5000 |
| **Coast Locking Multiplier** | 0.3 |

### Rear Differential
| Parameter | Value |
|---|---|
| **Diff Type** | LimitedSlip |
| **Slip Tolerance** | 2 |
| **Gear Ratio** | 1 |
| **Inertia** | 0.15 |
| **Power Bias** | 0.5 |
| **Preload L S D** | 0.2 |
| **Locking Friction** | 2000 |
| **Locking Stiffness** | 5000 |
| **Coast Locking Multiplier** | 0.5 |

## Front Axle
---
### Wheel Config
| Parameter | Value |
|---|---|
| **Mass** | 32 |
| **Radius** | 0.37 |
| **Inertia** | 2.2 |

### Tire Compound
| Parameter | Value |
|---|---|
| **Max Load Capacity** | 18000 |
| **Friction Multiplier** | 1.05 |
| **Rolling Resistance** | 0.015 |
| **Load Sensitivity** | 0.3 |
| **Long Relaxation Length** | 0.2 |
| **Lat Relaxation Length** | 0.35 |
| **Long B** | 14 |
| **Long C** | 1.6 |
| **Long D** | 1 |
| **Long E** | 0.98 |
| **Lat B** | 8 |
| **Lat C** | 1.3 |
| **Lat D** | 1 |
| **Lat E** | -0.2 |

### Brakes
| Parameter | Value |
|---|---|
| **Max Brake Torque** | 3200 |
| **Max Handbrake Torque** | 4500 |
| **Has A B S** | Yes |
| **Abs Slip Threshold** | 0.15 |
| **Abs Release Multiplier** | 0.3 |

### Suspension
| Parameter | Value |
|---|---|
| **Target Ride Height** | 0.45 |
| **Bump Travel** | 0.18 |
| **Droop Travel** | 0.12 |
| **Spring Stiffness** | 55000 |
| **Bump Damping** | 4500 |
| **Rebound Damping** | 6500 |
| **Blend Window** | 0.05 |
| **Bump Stop Gap** | 0.03 |
| **Bump Stop Stiffness** | 250000 |
| **Absolute Max Force** | 300000 |
| **Camber Gain Per Meter** | 6 |
| **Bump Steer Per Meter** | -2 |

## Rear Axle
---
### Wheel Config
| Parameter | Value |
|---|---|
| **Mass** | 32 |
| **Radius** | 0.37 |
| **Inertia** | 2.2 |

### Tire Compound
| Parameter | Value |
|---|---|
| **Max Load Capacity** | 18000 |
| **Friction Multiplier** | 1.05 |
| **Rolling Resistance** | 0.015 |
| **Load Sensitivity** | 0.3 |
| **Long Relaxation Length** | 0.2 |
| **Lat Relaxation Length** | 0.35 |
| **Long B** | 14 |
| **Long C** | 1.6 |
| **Long D** | 1 |
| **Long E** | 0.98 |
| **Lat B** | 8 |
| **Lat C** | 1.3 |
| **Lat D** | 1 |
| **Lat E** | -0.2 |

### Brakes
| Parameter | Value |
|---|---|
| **Max Brake Torque** | 3200 |
| **Max Handbrake Torque** | 4500 |
| **Has A B S** | Yes |
| **Abs Slip Threshold** | 0.15 |
| **Abs Release Multiplier** | 0.3 |

### Suspension
| Parameter | Value |
|---|---|
| **Target Ride Height** | 0.45 |
| **Bump Travel** | 0.15 |
| **Droop Travel** | 0.15 |
| **Spring Stiffness** | 35000 |
| **Bump Damping** | 3000 |
| **Rebound Damping** | 5500 |
| **Blend Window** | 0.05 |
| **Bump Stop Gap** | 0.02 |
| **Bump Stop Stiffness** | 250000 |
| **Absolute Max Force** | 300000 |
| **Camber Gain Per Meter** | 4 |
| **Bump Steer Per Meter** | 0 |

