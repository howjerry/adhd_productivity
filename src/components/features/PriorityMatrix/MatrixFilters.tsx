import React from 'react';
import { EnergyLevel } from '@/types';
import clsx from 'clsx';
import { Zap } from 'lucide-react';

interface MatrixFiltersProps {
  energyFilter: EnergyLevel | null;
  tagFilter: string | null;
  availableTags: string[];
  onEnergyFilterChange: (level: EnergyLevel | null) => void;
  onTagFilterChange: (tag: string | null) => void;
}

export const MatrixFilters: React.FC<MatrixFiltersProps> = ({
  energyFilter,
  tagFilter,
  availableTags,
  onEnergyFilterChange,
  onTagFilterChange,
}) => {
  return (
    <div className="matrix-filters">
      <span className="text-sm text-gray-600 mr-2">Filters:</span>
      
      {/* Energy Level Filter */}
      <button
        className={clsx('filter-button', { 'filter-active': !energyFilter })}
        onClick={() => onEnergyFilterChange(null)}
      >
        All Energy
      </button>
      {Object.values(EnergyLevel).map(level => (
        <button
          key={level}
          className={clsx('filter-button', { 'filter-active': energyFilter === level })}
          onClick={() => onEnergyFilterChange(energyFilter === level ? null : level)}
        >
          <Zap className="w-3 h-3 mr-1" />
          {level}
        </button>
      ))}

      {/* Tag Filter */}
      {availableTags.length > 0 && (
        <>
          <span className="text-gray-300">|</span>
          <button
            className={clsx('filter-button', { 'filter-active': !tagFilter })}
            onClick={() => onTagFilterChange(null)}
          >
            All Tags
          </button>
          {availableTags.slice(0, 5).map(tag => (
            <button
              key={tag}
              className={clsx('filter-button', { 'filter-active': tagFilter === tag })}
              onClick={() => onTagFilterChange(tagFilter === tag ? null : tag)}
            >
              #{tag}
            </button>
          ))}
        </>
      )}
    </div>
  );
};