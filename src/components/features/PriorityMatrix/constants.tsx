import React from 'react';
import { AlertTriangle, Target, Clock, Pause } from 'lucide-react';
import { MatrixQuadrant } from './MatrixQuadrant';

export const quadrants: MatrixQuadrant[] = [
  {
    id: 'urgent-important',
    title: 'Do First',
    description: 'Urgent & Important',
    icon: <AlertTriangle className="w-5 h-5" />,
    urgent: true,
    important: true,
    color: 'red',
  },
  {
    id: 'not-urgent-important',
    title: 'Schedule',
    description: 'Not Urgent & Important',
    icon: <Target className="w-5 h-5" />,
    urgent: false,
    important: true,
    color: 'blue',
  },
  {
    id: 'urgent-not-important',
    title: 'Delegate',
    description: 'Urgent & Not Important',
    icon: <Clock className="w-5 h-5" />,
    urgent: true,
    important: false,
    color: 'yellow',
  },
  {
    id: 'not-urgent-not-important',
    title: 'Eliminate',
    description: 'Not Urgent & Not Important',
    icon: <Pause className="w-5 h-5" />,
    urgent: false,
    important: false,
    color: 'green',
  },
];